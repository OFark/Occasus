using Microsoft.Data.SqlClient;

namespace Occasus.SQLRepository;

public class SQLSourceSettings
{
    public string? ConnectionString { get; set; }
    public SQLSourceSettings()
    {

    }

    public SQLSourceSettings(Action<SqlConnectionStringBuilder> builder)
    {
        var scsb = new SqlConnectionStringBuilder();
        builder.Invoke(scsb);

        ConnectionString = scsb.ToString();
    }

    public string TableName { get; set; } = "Settings";
    public string KeyColumnName { get; set; } = "Key";
    public string ValueColumnName { get; set; } = "Value";
    public bool EncryptSettings { get; set; } = false;
    public string? EncryptionKey = null;

    public SQLSourceSettings WithSQLConnection(Action<SqlConnectionStringBuilder> builder)
    {
        var scsb = new SqlConnectionStringBuilder();
        builder.Invoke(scsb);

        ConnectionString = scsb.ToString();
        return this;
    }

}
