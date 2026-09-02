using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace FinanceDashboardApi.Common;

public static class ConnectionStringBuilder
{
    /// <summary>Converts a postgres:// DATABASE_URL (Neon/Heroku-style) into an Npgsql connection string.</summary>
    public static string FromDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        var query = QueryHelpers.ParseQuery(uri.Query);
        var sslModeValue = query.TryGetValue("sslmode", out var sslValues) ? sslValues.ToString() : "require";
        var sslMode = sslModeValue switch
        {
            "disable" => SslMode.Disable,
            "prefer" => SslMode.Prefer,
            _ => SslMode.Require
        };

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = sslMode
        };

        return builder.ConnectionString;
    }
}
