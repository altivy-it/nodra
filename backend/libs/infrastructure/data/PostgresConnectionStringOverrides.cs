using Npgsql;

namespace Nodra.Backend.Infra.Data.Postgres;

public static class PostgresConnectionStringOverrides
{
    public static string WithHost(string connectionString, string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Host = host
        };

        return builder.ConnectionString;
    }
}
