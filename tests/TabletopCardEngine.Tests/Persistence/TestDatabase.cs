using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.PostgreSql;
using TabletopCardEngine.Sqlite;
using TabletopCardEngine.SqlServer;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace TabletopCardEngine.Tests.Persistence;

/// <summary>The databases the provider matrix runs on, one row per supported provider package.</summary>
public enum TestDatabaseKind
{
    Sqlite,
    InMemory,
    PostgreSql,
    SqlServer,
}

/// <summary>
/// Gives a test its own isolated database of a given kind, so the matrix tests keep the assembly's
/// method-level parallelism.
/// </summary>
/// <remarks>
/// PostgreSQL and SQL Server run in Docker, one container per kind for the whole test run, started on
/// first use and only under <see cref="ContainersVariable"/>=1. Each <see cref="Configure"/> call gets a
/// database of its own on that server, which <c>MigrateAsync</c> creates; the databases are left to die
/// with the container, which <see cref="TestContainersCleanup"/> disposes.
/// </remarks>
internal static class TestDatabase
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

    /// <summary>Whether the kind is a relational database with migrations of its own.</summary>
    internal static bool IsRelational(TestDatabaseKind kind) => kind != TestDatabaseKind.InMemory;

    /// <summary>
    /// Returns the registration call for a fresh database of <paramref name="kind"/>. The action can be
    /// applied to several containers to put them over the same database; <paramref name="lifetime"/>
    /// releases what the database holds (the kept-alive connection) and must outlive all of them.
    /// </summary>
    internal static Action<DbContextOptionsBuilder> Configure(TestDatabaseKind kind, out IDisposable lifetime)
    {
        switch (kind)
        {
            case TestDatabaseKind.Sqlite:
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                lifetime = connection;
                return options => options.UseHoldemSqlite(connection);
            case TestDatabaseKind.InMemory:
                var databaseName = Guid.NewGuid().ToString("N");
                lifetime = NoopLifetime.Instance;
                return options => options.UseHoldemInMemory(databaseName);
            case TestDatabaseKind.PostgreSql:
                RequireContainers();
                var postgreSql = new NpgsqlConnectionStringBuilder(_postgreSql.Value.GetAwaiter().GetResult().GetConnectionString())
                {
                    Database = NewDatabaseName(),

                    // On Windows Npgsql first asks for GSS encryption through SSPI, and on a domain-joined
                    // machine that handshake can eat the whole 15 s connect timeout when several tests
                    // open connections at once; the container offers no GSS anyway.
                    GssEncryptionMode = GssEncryptionMode.Disable,
                }.ConnectionString;
                lifetime = NoopLifetime.Instance;
                return options => options.UseHoldemPostgreSql(postgreSql);
            case TestDatabaseKind.SqlServer:
                RequireContainers();
                var sqlServer = new SqlConnectionStringBuilder(_sqlServer.Value.GetAwaiter().GetResult().GetConnectionString())
                {
                    InitialCatalog = NewDatabaseName(),
                }.ConnectionString;
                lifetime = NoopLifetime.Instance;
                return options => options.UseHoldemSqlServer(sqlServer);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a database kind of the matrix.");
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

    private sealed class NoopLifetime : IDisposable
    {
        internal static readonly NoopLifetime Instance = new();

        public void Dispose()
        {
        }
    }
}
