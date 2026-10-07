using Microsoft.Data.SqlClient;
using Npgsql;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace TabletopCardEngine.Server.Tests;

/// <summary>
/// Gives a test its own isolated database for each provider the server supports, so the end-to-end
/// tests keep the assembly's method-level parallelism.
/// </summary>
/// <remarks>
/// PostgreSQL and SQL Server run in Docker, one container per kind for the whole test run, started on
/// first use and only under <see cref="ContainersVariable"/>=1. Each <see cref="ConnectionStringFor"/>
/// call names a database of its own, which the server's start-up migration creates; the databases die
/// with the container, which <see cref="ServerContainersCleanup"/> disposes.
/// </remarks>
internal static class ServerDatabases
{
    /// <summary>Set to 1 to run the PostgreSQL and SQL Server rows; needs Docker.</summary>
    internal const string ContainersVariable = "TCE_TEST_CONTAINERS";

    private static readonly Lazy<Task<PostgreSqlContainer>> _postgreSql = new(() => StartAsync(
        new PostgreSqlBuilder("postgres:16-alpine").Build()));

    private static readonly Lazy<Task<MsSqlContainer>> _sqlServer = new(() => StartAsync(
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build()));

    /// <summary>Whether <see cref="ContainersVariable"/> switches the server kinds on.</summary>
    internal static bool ContainersEnabled
        => Environment.GetEnvironmentVariable(ContainersVariable) is "1" or "true";

    /// <summary>
    /// Returns a connection string to a fresh database of <paramref name="provider"/>: a new temporary
    /// file for SQLite (the caller deletes it), a new <c>tce_&lt;guid&gt;</c> database on the container's
    /// server otherwise. The server kinds report the test inconclusive without <see cref="ContainersVariable"/>.
    /// </summary>
    internal static string ConnectionStringFor(HoldemServerProvider provider)
    {
        switch (provider)
        {
            case HoldemServerProvider.Sqlite:
                return $"Data Source={Path.Combine(Path.GetTempPath(), $"tce-server-{Guid.NewGuid():N}.db")}";
            case HoldemServerProvider.PostgreSql:
                RequireContainers();
                return new NpgsqlConnectionStringBuilder(_postgreSql.Value.GetAwaiter().GetResult().GetConnectionString())
                {
                    Database = NewDatabaseName(),
                }.ConnectionString;
            case HoldemServerProvider.SqlServer:
                RequireContainers();
                return new SqlConnectionStringBuilder(_sqlServer.Value.GetAwaiter().GetResult().GetConnectionString())
                {
                    InitialCatalog = NewDatabaseName(),
                }.ConnectionString;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Not a provider of the server.");
        }
    }

    /// <summary>Disposes the containers this run started; one never asked for is not started now.</summary>
    internal static async Task DisposeContainersAsync()
    {
        if (_postgreSql.IsValueCreated)
        {
            await (await _postgreSql.Value).DisposeAsync();
        }

        if (_sqlServer.IsValueCreated)
        {
            await (await _sqlServer.Value).DisposeAsync();
        }
    }

    private static void RequireContainers()
    {
        if (!ContainersEnabled)
        {
            Assert.Inconclusive($"Set {ContainersVariable}=1 (needs Docker) to run PostgreSQL and SQL Server");
        }
    }

    private static string NewDatabaseName() => $"tce_{Guid.NewGuid():N}";

    private static async Task<TContainer> StartAsync<TContainer>(TContainer container)
        where TContainer : DotNet.Testcontainers.Containers.IContainer
    {
        await container.StartAsync();
        return container;
    }
}
