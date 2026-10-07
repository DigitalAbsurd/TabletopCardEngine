using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.PostgreSql;
using TabletopCardEngine.Sqlite;
using TabletopCardEngine.SqlServer;

namespace TabletopCardEngine.Server;

internal static class HoldemServerDatabase
{
    internal const string ProviderKey = "HoldemServer:Provider";
    internal const string ConnectionStringName = "Holdem";

    internal static Action<DbContextOptionsBuilder> Configure(IConfiguration configuration)
    {
        var providerValue = configuration[ProviderKey] ?? string.Empty;
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        var validProviders = Enum.GetNames<HoldemServerProvider>();
        var provider = validProviders.FirstOrDefault(p => p.Equals(providerValue, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            throw new InvalidOperationException($"HoldemServer:Provider must be one of {string.Join(", ", validProviders)} (environment variable HoldemServer__Provider); got '{providerValue}'.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Holdem is not set (environment variable ConnectionStrings__Holdem).");
        }

        var providerEnum = Enum.Parse<HoldemServerProvider>(provider, ignoreCase: true);
        return providerEnum switch
        {
            HoldemServerProvider.Sqlite => db => db.UseHoldemSqlite(connectionString),
            HoldemServerProvider.PostgreSql => db => db.UseHoldemPostgreSql(connectionString),
            HoldemServerProvider.SqlServer => db => db.UseHoldemSqlServer(connectionString),
            _ => throw new InvalidOperationException($"Unknown provider: {providerEnum}"),
        };
    }
}
