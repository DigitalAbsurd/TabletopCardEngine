using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Data;
using TabletopCardEngine.Holdem;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The public schema entry point over the test host's migrations via <c>UseHoldemSqlite</c>.
/// </summary>
[TestClass]
public sealed class SchemaManagerTests
{
    [TestMethod]
    public void SchemaManager_IsRegisteredPerContainer()
    {
        using var first = TestEngineHost.Setup(out _, out _);
        using var second = TestEngineHost.Setup(out _, out _);

        var firstManager = first.GetRequiredService<IHoldemSchemaManager>();

        firstManager.Should().BeSameAs(first.GetRequiredService<IHoldemSchemaManager>());
        firstManager.Should().NotBeSameAs(second.GetRequiredService<IHoldemSchemaManager>());
    }

    [TestMethod]
    public async Task SchemaManager_PlainUseSqlite_FailsWithMigrationsNotFound()
    {
        using var host = SetupPlainUseSqlite();
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var thrown = await manager.Invoking(schemaManager => schemaManager.MigrateAsync())
            .Should().ThrowExactlyAsync<HoldemSchemaException>();

        thrown.Which.Failure.Should().Be(HoldemSchemaFailure.MigrationsNotFound);
        thrown.Which.Message.Should().Contain("UseHoldemSqlite");
    }

    [TestMethod]
    public async Task SchemaManager_Status_OnAMigratedDatabase()
    {
        using var host = TestEngineHost.Setup(out _, out _);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.UpToDate);
        status.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        status.PendingMigrations.Should().BeEmpty();
        status.EngineSchemaVersion.Should().Be(1);
        status.DatabaseSchemaVersion.Should().Be(1);
        status.DatabaseEngineVersion.Should().Be("1.0.0");
        status.Should().Be(await manager.GetSchemaStatusAsync(), "two reads of the same database compare equal");
    }

    [TestMethod]
    public async Task SchemaStatus_Migrated_ReportsTheEngineVersion()
    {
        using var host = TestEngineHost.Setup(out _, out _);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.DatabaseSchemaVersion.Should().Be(1);
        status.DatabaseEngineVersion.Should().Be("1.0.0");
        status.State.Should().Be(HoldemSchemaState.UpToDate);
    }

    [TestMethod]
    public async Task SchemaStatus_NewerDatabase_IsNewerThanEngine()
    {
        using var host = TestEngineHost.Setup(out _, out _, seedAction: ctx =>
        {
            var row = ctx.Set<TceSchemaRow>().First();
            row.SchemaVersion = 2;
            ctx.SaveChanges();
        });
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.NewerThanEngine);
    }

    [TestMethod]
    public async Task SchemaStatus_OlderDatabase_NeedsMigration()
    {
        using var host = TestEngineHost.Setup(out _, out _, seedAction: ctx =>
        {
            var row = ctx.Set<TceSchemaRow>().First();
            row.SchemaVersion = 0;
            ctx.SaveChanges();
        });
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.MigrationRequired);
    }

    [TestMethod]
    public async Task SchemaStatus_MissingRow_NeedsMigration()
    {
        using var host = TestEngineHost.Setup(out _, out _, seedAction: ctx =>
        {
            var row = ctx.Set<TceSchemaRow>().First();
            ctx.Set<TceSchemaRow>().Remove(row);
            ctx.SaveChanges();
        });
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.MigrationRequired);
        status.DatabaseSchemaVersion.Should().BeNull();
    }

    [TestMethod]
    [DataRow(null, false, HoldemSchemaState.MigrationRequired)]
    [DataRow(null, true, HoldemSchemaState.MigrationRequired)]
    [DataRow(0, false, HoldemSchemaState.MigrationRequired)]
    [DataRow(0, true, HoldemSchemaState.MigrationRequired)]
    [DataRow(1, false, HoldemSchemaState.UpToDate)]
    [DataRow(1, true, HoldemSchemaState.MigrationRequired)]
    [DataRow(2, false, HoldemSchemaState.NewerThanEngine)]
    [DataRow(2, true, HoldemSchemaState.NewerThanEngine)]
    public void SchemaCompare_Table(int? databaseVersion, bool hasPendingMigrations, HoldemSchemaState expected)
    {
        var result = HoldemSchema.Compare(databaseVersion, hasPendingMigrations);

        result.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("Microsoft.EntityFrameworkCore.Sqlite", true)]
    [DataRow("Npgsql.EntityFrameworkCore.PostgreSQL", true)]
    [DataRow("Microsoft.EntityFrameworkCore.SqlServer", true)]
    [DataRow("Microsoft.EntityFrameworkCore.InMemory", true)]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("Pomelo.EntityFrameworkCore.MySql", false)]
    [DataRow("Microsoft.EntityFrameworkCore.Cosmos", false)]
    [DataRow("microsoft.entityframeworkcore.sqlite", false)]
    public void SchemaManager_UnknownProviderName_IsUnsupported(string? providerName, bool expected)
    {
        HoldemProviders.IsSupported(providerName).Should().Be(expected);
    }

    private static TestEngineHost.DisposableTestData SetupPlainUseSqlite()
    {
        var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        IServiceCollection services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseSqlite(keepAliveConnection),
            configure: _ => { }
        );
        var serviceProvider = services.BuildServiceProvider();
        keepAliveConnection.Open();
        using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContext();
        dbContext.Database.EnsureCreated();
        return new TestEngineHost.DisposableTestData(serviceProvider, keepAliveConnection);
    }
}
