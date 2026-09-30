using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Options;
using OpenHoldemEngine.Sqlite;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// Spins up a real DI container over SQLite for a single test.
/// Shared by every test class, so it lives here rather than inside one of them.
/// </summary>
internal static class TestEngineHost
{
    /// <summary>
    /// Writes the SQLite database to this file instead of <c>:memory:</c>, so the schema and rows
    /// can be inspected after a run.
    /// <para>
    /// Setting it implies a serial run. Every test then shares that one file, and each
    /// <see cref="Setup"/> deletes and recreates it, which under the assembly's method-level
    /// parallelism pulls the database out from under whichever test is still running. Use it with a
    /// <c>--filter</c> that selects a single test.
    /// </para>
    /// </summary>
    internal const string Db3FileNameVariable = "OHE_TEST_SQLITE_DB3_FILE_NAME";

    /// <summary>Logs EF SQL with sensitive data to the debug output (DEBUG builds only).</summary>
    internal const string SqlDebugVariable = "OHE_TEST_SQL_DEBUG";

    internal static DisposableTestData Setup(out HoldemEngine holdemEngine,
        out IDbContextFactory<HoldemDbContext> contextFactory,
        ICardShuffler? shuffler = null,
        Action<HoldemEngineSettings>? configureSettings = null,
        Action<HoldemDbContext>? seedAction = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var db3FileName = Environment.GetEnvironmentVariable(Db3FileNameVariable);
        var keepAliveConnection = string.IsNullOrEmpty(db3FileName) ?
            new SqliteConnection("DataSource=:memory:") :
            default;

        if (keepAliveConnection == default && db3FileName != null)
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(db3FileName))
            {
                File.Delete(db3FileName);
            }
        }

        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts =>
            {
#if DEBUG
                if (bool.TryParse(Environment.GetEnvironmentVariable(SqlDebugVariable), out var dbgSql) && dbgSql)
                {
                    opts.LogTo(msg => System.Diagnostics.Debug.WriteLine(msg), minimumLevel: Microsoft.Extensions.Logging.LogLevel.Information);
                    opts.EnableSensitiveDataLogging();
                }
#endif
                if (keepAliveConnection != default)
                {
                    opts.UseHoldemSqlite(keepAliveConnection);
                }
                else
                {
                    opts.UseHoldemSqlite($"Filename='{db3FileName}'");
                }
            },
            configure: opts =>
            {
                if (configureSettings != null)
                {
                    opts.WithSettings(configureSettings);
                }

                if (shuffler != null)
                {
                    opts.WithCardShuffler(shuffler);
                }
            }
            );
        configureServices?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();
        keepAliveConnection?.Open();
        serviceProvider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>();
        using var dbContext = contextFactory.CreateDbContext();
        seedAction?.Invoke(dbContext);

        holdemEngine = serviceProvider.GetRequiredService<HoldemEngine>();
        return new DisposableTestData(serviceProvider, keepAliveConnection);
    }

    internal sealed class DisposableTestData : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly SqliteConnection? _sqliteConnection;

        internal DisposableTestData(ServiceProvider serviceProvider, SqliteConnection? sqliteConnection)
        {
            _serviceProvider = serviceProvider;
            _sqliteConnection = sqliteConnection;
        }

        internal T GetRequiredService<T>() where T : notnull
            => _serviceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            _sqliteConnection?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}
