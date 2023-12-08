using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Occasus.Helpers;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using Occasus.Settings.Models;
using Occasus.SQLEFRepository.Models;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Occasus.SQLEFRepository;

public class SQLEFSettingsRepository : SettingsRepositoryBase, IOptionsStorageRepositoryWithServices
{
    internal readonly SQLEFSourceSettings SQLSettings;

    readonly DbContextOptionsBuilder<OccasusContext> DBContextOptionsBuilder;

    internal SQLEFSettingsRepository(Action<SQLEFSourceSettings> settings)
    {
        SQLSettings = new();

        settings(SQLSettings);

        if (SQLSettings.ConnectionString is null)
        {
            throw new ArgumentNullException(nameof(SQLSettings.ConnectionString), $"Connection String can either be built with the {nameof(SQLSettings.WithSQLConnection)} parameter, or specified directly in the {nameof(SQLEFSourceSettings.ConnectionString)} parameter");
        }

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey?.Length < 12)
        {
            SQLSettings.EncryptSettings = false;
            Messages = new()
            {
                "Encryption Disabled: Encryption key must be at least 12 characters"
            };
        }

        DBContextOptionsBuilder = new();
        DBContextOptionsBuilder.UseSqlServer(SQLSettings.ConnectionString, SQLSettings.SqlServerDbContextOptionsBuilder);

    }

    internal SQLEFSettingsRepository(Action<SQLEFSourceSettings> settings, Action<DbContextOptionsBuilder<OccasusContext>> context)
    {
        SQLSettings = new();

        settings(SQLSettings);

        if (SQLSettings.ConnectionString is null)
        {
            throw new ArgumentNullException(nameof(SQLSettings.ConnectionString), $"Connection String can either be built with the {nameof(SQLSettings.WithSQLConnection)} parameter, or specified directly in the {nameof(SQLEFSourceSettings.ConnectionString)} parameter");
        }

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey?.Length < 12)
        {
            SQLSettings.EncryptSettings = false;
            Messages = new()
            {
                "Encryption Disabled: Encryption key must be at least 12 characters"
            };
        }

        DBContextOptionsBuilder = new();
        context(DBContextOptionsBuilder);
    }

    private string CheckTableExistsQuery => $"SELECT COUNT(*) FROM information_schema.TABLES WHERE (TABLE_NAME = '{SQLSettings.TableName}')";
    private string CreateTableCommand => $"CREATE TABLE dbo.[{SQLSettings.TableName}] ([{SQLSettings.KeyColumnName}] varchar(255) NOT NULL,[{SQLSettings.ValueColumnName}] nvarchar(MAX) NOT NULL) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] ADD CONSTRAINT PK_{SQLSettings.TableName.Replace(" ", "_")} PRIMARY KEY CLUSTERED([{SQLSettings.KeyColumnName}]) WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] SET(LOCK_ESCALATION = TABLE)";
    OccasusContext CreateDbContext() => new(DBContextOptionsBuilder.Options, SQLSettings);
    private string LoadQuery => $"SELECT [{SQLSettings.KeyColumnName}], [{SQLSettings.ValueColumnName}] FROM [{SQLSettings.TableName}]";

    public override async Task ClearSettings(string? className = null, CancellationToken cancellation = default)
    {
        await DeleteSettings(className, cancellation).ConfigureAwait(false);

        OnReload();
    }

    public override IDictionary<string, string> LoadSettings()
    {
        Debug.Assert(SQLSettings is not null);

        EnsureDBExists();
        return ReadSettingsFromDB();
    }

    public override Task ReloadSettings(CancellationToken cancellation = default)
    {
        Logger?.LogTrace("Reloading Settings from SQL");
        OnReload();

        return Task.CompletedTask;
    }

    public override async Task StoreSetting<T>(T value, Type valueType, CancellationToken cancellation = default)
    {
        Debug.Assert(SQLSettings is not null);
        Logger?.LogTrace("Saving settings to SQL");

        var className = valueType.Name;

        var settingItems = value?.ToSettingItems(className, Logger);

        if (settingItems is null)
        {
            return;
        }

        await DeleteSettings(className, cancellation).ConfigureAwait(false);

        var persisting = settingItems.Select(ss => PersistValue(ss, cancellation)).ToArray();

        Task.WaitAll(persisting, cancellation);

        await ReloadSettings(cancellation);
    }

    private async Task DeleteSettings(string? className = null, CancellationToken cancellation = default)
    {
        using var dbContext = CreateDbContext();

        if(className is null)
        {
            await dbContext.Settings.ExecuteDeleteAsync(cancellation).ConfigureAwait(false);
            return;
        }

        await dbContext.Settings.Where(x => x.Key.StartsWith($"{className}:")).ExecuteDeleteAsync(cancellation).ConfigureAwait(false);
    }
    private void EnsureDBExists()
    {
        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    private async Task<IDictionary<string, string>> ReadSettingsFromDBAsync(CancellationToken cancellation = default)
    {
        Debug.Assert(SQLSettings is not null);

        var dic = new Dictionary<string, string>();
        using var dbContext = CreateDbContext();

        return await dbContext.Settings.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value != null)
                          .ToDictionaryAsync(x => x.Key, DecryptValue, cancellation).ConfigureAwait(false);
    }

    private IDictionary<string, string> ReadSettingsFromDB()
    {
        Debug.Assert(SQLSettings is not null);

        var dic = new Dictionary<string, string>();
        using var dbContext = CreateDbContext();

        return dbContext.Settings.Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value != null)
                          .ToDictionary(x => x.Key, DecryptValue);
    }

    private string DecryptValue(Setting setting)
    {
        try
        {
            return SQLSettings.EncryptSettings ?
                                    AESThenHMAC.SimpleDecryptWithPassword(setting.Value, SQLSettings.EncryptionKey ?? "somepassword") ?? setting.Value :
                                    setting.Value;
        }
        catch (FormatException)
        {
            Logger?.LogWarning("Cannot decrypt value ({message}), possibly not encrypted", setting.Key);
            return setting.Value;
        }
    }

    private async Task PersistValue(SettingStorage ss, CancellationToken cancellation = default)
    {
        Logger?.LogTrace("Persisting Settings to SQL");
        //using var connection = new SqlConnection(SQLSettings.ConnectionString);
        //await connection.OpenAsync(cancellation).ConfigureAwait(false);
        var key = ss.Name;
        var itemValue = ss.Value;

        var settings = await ReadSettingsFromDBAsync(cancellation);

        if (itemValue is null)
        {
            return;
        }

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey is not null)
        {
            itemValue = AESThenHMAC.SimpleEncryptWithPassword(itemValue, SQLSettings.EncryptionKey);
        }

        using var dbContext = CreateDbContext();

        var keyPair = await dbContext.Settings.FirstOrDefaultAsync(x => x.Key == key, cancellation).ConfigureAwait(false);

        keyPair ??= (await dbContext.Settings.AddAsync(new Setting() { Key = key, Value = itemValue }, cancellation).ConfigureAwait(false)).Entity;

        keyPair.Value = itemValue;

        await dbContext.SaveChangesAsync(cancellation).ConfigureAwait(false);

        
        Logger?.LogTrace("Persisting Settings to SQL Complete");
    }
}
