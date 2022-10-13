using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;
using Occasus.Helpers;
using Occasus.Repository.Interfaces;
using Occasus.Settings;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Occasus.SQLRepository;

public class SQLSettingsRepository : IOptionsStorageRepository
{
    private CancellationTokenSource? cancellationTokenSource;

    private IChangeToken? changeToken;    

    public SQLSettingsRepository(WebApplicationBuilder builder, Action<SQLSourceSettings> settings)
    {
        Builder = builder;

        SQLSettings = new();

        settings(SQLSettings);

        if (SQLSettings.ConnectionString is null) throw new ArgumentNullException(nameof(SQLSettings.ConnectionString), $"Connection String can either be built with the {nameof(SQLSettings.WithSQLConnection)} parameter, or specified directly in the {nameof(SQLSourceSettings.ConnectionString)} parameter");

        if (SQLSettings.EncryptSettings && SQLSettings.EncryptionKey?.Length < 12)
        {
            SQLSettings.EncryptSettings = false;
            MessageStore.Messages.Add("Encryption Disabled: Encryption key must be at least 12 characters");
        }
    }

    public SQLSettingsRepository(WebApplicationBuilder builder, Action<SqlConnectionStringBuilder> sqlConnBuilder) : this(builder, b => { b.WithSQLConnection(sqlConnBuilder); })
    { }

    public WebApplicationBuilder Builder { get; }
    public SQLSourceSettings SQLSettings { get; }

    private string CheckTableExistsQuery => $"SELECT COUNT(*) FROM information_schema.TABLES WHERE (TABLE_NAME = '{SQLSettings.TableName}')";
    private string CreateTableCommand => $"CREATE TABLE dbo.[{SQLSettings.TableName}] ([{SQLSettings.KeyColumnName}] varchar(255) NOT NULL,[{SQLSettings.ValueColumnName}] nvarchar(MAX) NOT NULL) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] ADD CONSTRAINT PK_{SQLSettings.TableName.Replace(" ", "_")} PRIMARY KEY CLUSTERED([{SQLSettings.KeyColumnName}]) WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]; ALTER TABLE dbo.[{SQLSettings.TableName}] SET(LOCK_ESCALATION = TABLE)";
    private string LoadQuery => $"SELECT [{SQLSettings.KeyColumnName}], [{SQLSettings.ValueColumnName}] FROM [{SQLSettings.TableName}]";
    public async Task ClearSettings(string? classname = null, CancellationToken? token = null)
    {
        token ??= new CancellationTokenSource().Token;

        var command = $"DELETE FROM [{SQLSettings.TableName}]{(classname is not null ? $" WHERE [{SQLSettings.KeyColumnName}] LIKE @Filter" : "")}";

        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(command, connection);
        if (classname is not null)
        {
            query.Parameters.Add(new SqlParameter("@Filter", $"{classname}:%"));
        }
        query.Connection.Open();
        await query.ExecuteNonQueryAsync(token.Value);

        cancellationTokenSource?.Cancel();
    }

    public async Task DeleteSetting(string key, CancellationToken? token = null)
    {
        Debug.Assert(SQLSettings is not null);
        Debug.Assert(!string.IsNullOrWhiteSpace(key));

        token ??= new CancellationTokenSource().Token;

        var settings = await LoadSettingsAsync(token.Value);

        if (settings is null || !settings.ContainsKey(key)) return;

        var command = $"DELETE FROM [{SQLSettings.TableName}] WHERE [{SQLSettings.KeyColumnName}] = @Key";

        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(command, connection);
        query.Parameters.Add(new SqlParameter("@Key", key));

        query.Connection.Open();

        _ = await query.ExecuteNonQueryAsync(token.Value);

        cancellationTokenSource?.Cancel();
    }

    public IDictionary<string, string> LoadSettings()
    {
        Debug.Assert(SQLSettings is not null);

        EnsureDBExists();
        return ReadSettingsFromDB();
    }

    public Task ReloadSettings(CancellationToken? token = null)
    {
        cancellationTokenSource?.Cancel();

        return Task.CompletedTask;
    }

    public async Task StoreSetting(string key, string value, CancellationToken? token = null)
    {
        Debug.Assert(SQLSettings is not null);
        Debug.Assert(!string.IsNullOrWhiteSpace(key));

        token ??= new CancellationTokenSource().Token;

        var settings = await LoadSettingsAsync(token.Value);

        if (SQLSettings.EncryptSettings)
        {
            value = AESThenHMAC.SimpleEncryptWithPassword(value, SQLSettings.EncryptionKey ?? "somepassword");
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

        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(command, connection);
        query.Parameters.Add(new SqlParameter("@Key", key));
        query.Parameters.Add(new SqlParameter("@Value", value));

        query.Connection.Open();

        _ = await query.ExecuteNonQueryAsync(token.Value);

    }

    public IChangeToken Watch()
    {
        cancellationTokenSource = new CancellationTokenSource();
        changeToken = new CancellationChangeToken(cancellationTokenSource.Token);

        return changeToken;
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

    private async Task<IDictionary<string, string>> LoadSettingsAsync(CancellationToken? token = null)
    {
        Debug.Assert(SQLSettings is not null);

        token ??= new CancellationTokenSource().Token;

        var dic = new Dictionary<string, string>();
        using var connection = new SqlConnection(SQLSettings.ConnectionString);
        using var query = new SqlCommand(LoadQuery, connection);
        query.Connection.Open();
        using (var reader = await query.ExecuteReaderAsync(token.Value))
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
