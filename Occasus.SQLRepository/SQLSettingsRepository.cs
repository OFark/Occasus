using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Occasus.Helpers;
using Occasus.Options;
using Occasus.Repository.Interfaces;
using Occasus.Settings.Models;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Occasus.SQLRepository;

public class SQLSettingsRepository : SettingsRepositoryBase, IOptionsStorageRepositoryWithServices
{
    internal readonly SQLSourceSettings SQLSettings;

    internal SQLSettingsRepository(Action<SQLSourceSettings> settings)
    {
        SQLSettings = new();

        settings(SQLSettings);

        if (SQLSettings.ConnectionString is null)
        {
            throw new ArgumentNullException(nameof(SQLSettings.ConnectionString), $"Connection String can either be built with the {nameof(SQLSettings.WithSQLConnection)} parameter, or specified directly in the {nameof(SQLSourceSettings.ConnectionString)} parameter");
        }

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey?.Length < 12)
        {
            SQLSettings.EncryptSettings = false;
            Messages = new()
            {
                "Encryption Disabled: Encryption key must be at least 12 characters"
            };
        }

    }
    
    private string CheckTableExistsQuery => $"SELECT COUNT(*) FROM information_schema.TABLES WHERE (TABLE_NAME = '{SQLSettings.TableName}')";
    private string CreateTableCommand => $"CREATE TABLE dbo.[{SQLSettings.TableName}] ([{SQLSettings.KeyColumnName}] varchar(255) NOT NULL,[{SQLSettings.ValueColumnName}] nvarchar(MAX) NOT NULL) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] ADD CONSTRAINT PK_{SQLSettings.TableName.Replace(" ", "_")} PRIMARY KEY CLUSTERED([{SQLSettings.KeyColumnName}]) WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] SET(LOCK_ESCALATION = TABLE)";
    private string LoadQuery => $"SELECT [{SQLSettings.KeyColumnName}], [{SQLSettings.ValueColumnName}] FROM [{SQLSettings.TableName}]";

    public override async Task ClearSettings(string? className = null, CancellationToken cancellation = default)
    {
        await DeleteSettings(className, cancellation).ConfigureAwait(false);

        OnReload();
    }

    private async Task DeleteSettings(string? className = null, CancellationToken cancellation = default)
    {
        var command = $"DELETE FROM [{SQLSettings.TableName}]{(className is not null ? $" WHERE [{SQLSettings.KeyColumnName}] LIKE @Filter" : "")}";

        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(command, connection);
        if (className is not null)
        {
            query.Parameters.Add(new SqlParameter("@Filter", $"{className}:%"));
        }
        query.Connection.Open();
        await query.ExecuteNonQueryAsync(cancellation);
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

    private void EnsureDBExists()
    {
        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using (var query = new SqlCommand(CheckTableExistsQuery, connection))
        {
            query.Connection.Open();
            var count = query.ExecuteScalar();
            if (count is int tableCount && tableCount == 1)
            {
                return;
            }
        }

        using var command = new SqlCommand(CreateTableCommand, connection);
        command.ExecuteNonQuery();

    }

    private async Task<IDictionary<string, string>> LoadSettingsAsync(CancellationToken cancellation = default)
    {
        Debug.Assert(SQLSettings is not null);

        var dic = new Dictionary<string, string>();
        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(LoadQuery, connection);
        query.Connection.Open();
        using (var reader = await query.ExecuteReaderAsync(cancellation))
        {
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);

                if (SQLSettings.EncryptSettings)
                {
                    try
                    {
                        value = AESThenHMAC.SimpleDecryptWithPassword(value, SQLSettings.EncryptionKey ?? "somepassword");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Cannot decrypt value ({key}), possibly not encrypted");
                    }
                }

                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    dic.Add(key, value);
                }
            }
        }

        return dic;
    }

    private async Task PersistValue(SettingStorage ss, CancellationToken cancellation = default)
    {
        Logger?.LogTrace("Persisting Settings to SQL");
        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        await connection.OpenAsync(cancellation).ConfigureAwait(false);
        var key = ss.Name;
        var itemValue = ss.Value;

        var settings = await LoadSettingsAsync(cancellation);

        if (itemValue is null)
        {
            return;
        }

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey is not null)
        {
            itemValue = AESThenHMAC.SimpleEncryptWithPassword(itemValue, SQLSettings.EncryptionKey);
        }

        string? command;
        if (settings is null || !settings.ContainsKey(key))
        {
            command = $"INSERT INTO [{SQLSettings.TableName}] ([{SQLSettings.KeyColumnName}], [{SQLSettings.ValueColumnName}]) VALUES ( @Key, @Value )";
        }
        else
        {
            command = $"UPDATE [{SQLSettings.TableName}] SET [{SQLSettings.ValueColumnName}] = @Value WHERE [{SQLSettings.KeyColumnName}] = @Key";
        }

        using var query = new SqlCommand(command, connection);
        query.Parameters.Add(new SqlParameter("@Key", key));
        query.Parameters.Add(new SqlParameter("@Value", itemValue));

        await query.ExecuteNonQueryAsync(cancellation).ConfigureAwait(false);
        Logger?.LogTrace("Persisting Settings to SQL Complete");
    }
    private IDictionary<string, string> ReadSettingsFromDB()
    {
        var dic = new Dictionary<string, string>();
        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(LoadQuery, connection);

        query.Connection.Open();
        using (var reader = query.ExecuteReader())
        {
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);

                if (SQLSettings.EncryptSettings)
                {
                    try
                    {
                        value = AESThenHMAC.SimpleDecryptWithPassword(value, SQLSettings.EncryptionKey ?? "somepassword");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Cannot decrypt value ({key}), possibly not encrypted");
                    }
                }

                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    dic.Add(key, value);
                }
            }
        }

        return dic;
    }
}
