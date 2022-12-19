﻿using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.SqlClient;

namespace Occasus.SQLEFRepository;

public class SQLEFSourceSettings
{
    public string? ConnectionString { get; set; }
    public SQLEFSourceSettings()
    {

    }

    public SQLEFSourceSettings(Action<SqlConnectionStringBuilder> builder, Action<SqlServerDbContextOptionsBuilder>? sqlServerDbContextOptionsBuilder = null)
    {
        var scsb = new SqlConnectionStringBuilder();
        builder.Invoke(scsb);

        ConnectionString = scsb.ToString();
        SqlServerDbContextOptionsBuilder = sqlServerDbContextOptionsBuilder;
    }

    public string TableName { get; set; } = "Settings";
    public string KeyColumnName { get; set; } = "Key";
    public string ValueColumnName { get; set; } = "Value";
    public bool EncryptSettings { get; set; } = false;
    public string? EncryptionKey = null;

    public Action<SqlServerDbContextOptionsBuilder>? SqlServerDbContextOptionsBuilder { get; set; }

    public SQLEFSourceSettings WithSQLConnection(Action<SqlConnectionStringBuilder> builder, Action<SqlServerDbContextOptionsBuilder>? sqlServerDbContextOptionsBuilder = null)
    {
        var scsb = new SqlConnectionStringBuilder();
        builder.Invoke(scsb);

        ConnectionString = scsb.ToString();
        SqlServerDbContextOptionsBuilder ??= sqlServerDbContextOptionsBuilder;
        return this;
    }

}
