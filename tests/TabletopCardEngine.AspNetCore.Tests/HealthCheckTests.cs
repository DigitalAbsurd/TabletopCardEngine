using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TabletopCardEngine.InMemory;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.AspNetCore.Tests;

[TestClass]
public sealed class HealthCheckTests
{
    private const string CheckName = "tabletopcardengine";

    private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> dbOptionsAction, HealthStatus? failureStatus = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHoldemEngine(dbOptionsAction);
        services.AddHealthChecks().AddHoldemEngineCheck(failureStatus: failureStatus);
        return services.BuildServiceProvider();
    }

    private static Task<HealthReport> CheckAsync(IServiceProvider provider)
    {
        return provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
    }

    private static Task MigrateAsync(IServiceProvider provider)
    {
        return provider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync();
    }

    private static SqliteConnection OpenMemoryConnection()
    {
        // Kept open for the whole test, so the :memory: database outlives each DbContext.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    [TestMethod]
    public async Task HealthCheck_InMemoryMigrated_IsHealthyAndNotVersioned()
    {
        await using var provider = BuildProvider(db => db.UseHoldemInMemory(Guid.NewGuid().ToString("N")));
        await MigrateAsync(provider);

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries[CheckName].Data["state"].Should().Be(nameof(HoldemSchemaState.NotVersioned));
    }

    [TestMethod]
    public async Task HealthCheck_SqliteMigrated_IsHealthyAndUpToDate()
    {
        using var connection = OpenMemoryConnection();
        await using var provider = BuildProvider(db => db.UseHoldemSqlite(connection));
        await MigrateAsync(provider);

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Healthy);
        var entry = report.Entries[CheckName];
        entry.Data["state"].Should().Be(nameof(HoldemSchemaState.UpToDate));
        entry.Data["pendingMigrations"].Should().Be(0);
        entry.Data.Should().ContainKey("databaseSchemaVersion");
    }

    [TestMethod]
    public async Task HealthCheck_SqliteNotMigrated_IsUnhealthyAndMigrationRequired()
    {
        using var connection = OpenMemoryConnection();
        await using var provider = BuildProvider(db => db.UseHoldemSqlite(connection));

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Unhealthy);
        var entry = report.Entries[CheckName];
        entry.Data["state"].Should().Be(nameof(HoldemSchemaState.MigrationRequired));
        ((int)entry.Data["pendingMigrations"]).Should().BeGreaterOrEqualTo(1);
        entry.Data.Should().NotContainKey("databaseSchemaVersion");
        entry.Exception.Should().BeNull();
    }

    [TestMethod]
    public async Task HealthCheck_SqliteMigratedByNewerEngine_IsUnhealthyAndNewerThanEngine()
    {
        using var connection = OpenMemoryConnection();
        await using var provider = BuildProvider(db => db.UseHoldemSqlite(connection));
        await MigrateAsync(provider);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE \"__OheSchema\" SET \"SchemaVersion\" = 999";
            await command.ExecuteNonQueryAsync();
        }

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Unhealthy);
        var entry = report.Entries[CheckName];
        entry.Data["state"].Should().Be(nameof(HoldemSchemaState.NewerThanEngine));
        entry.Data["databaseSchemaVersion"].Should().Be(999);
    }

    [TestMethod]
    public async Task HealthCheck_DatabaseFileMissing_IsUnhealthy()
    {
        // EF Core's SQLite provider decides a database exists by looking for the file, without opening
        // it, so a missing file reads as a database nothing was applied to: unhealthy, no exception.
        var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing", "holdem.db");
        await using var provider = BuildProvider(db => db.UseHoldemSqlite("Data Source=" + missingFile));

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Unhealthy);
        report.Entries[CheckName].Data["state"].Should().Be(nameof(HoldemSchemaState.MigrationRequired));
    }

    [TestMethod]
    public async Task HealthCheck_DatabaseUnreadable_IsUnhealthyWithTheException()
    {
        // A file that exists but is not a SQLite database: the provider throws on the first query, and
        // the health check service reports that exception, since the check does not catch it.
        var unreadableFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
        await File.WriteAllTextAsync(unreadableFile, "This is not a SQLite database, and it is long enough to have a header.");
        try
        {
            await using var provider = BuildProvider(db => db.UseHoldemSqlite("Data Source=" + unreadableFile + ";Pooling=False"));

            var report = await CheckAsync(provider);

            report.Status.Should().Be(HealthStatus.Unhealthy);
            report.Entries[CheckName].Exception.Should().NotBeNull();
        }
        finally
        {
            File.Delete(unreadableFile);
        }
    }

    [TestMethod]
    public async Task HealthCheck_FailureStatusDegraded_ReportsDegradedWhenNotMigrated()
    {
        using var connection = OpenMemoryConnection();
        await using var provider = BuildProvider(db => db.UseHoldemSqlite(connection), HealthStatus.Degraded);

        var report = await CheckAsync(provider);

        report.Status.Should().Be(HealthStatus.Degraded);
        report.Entries[CheckName].Data["state"].Should().Be(nameof(HoldemSchemaState.MigrationRequired));
    }
}
