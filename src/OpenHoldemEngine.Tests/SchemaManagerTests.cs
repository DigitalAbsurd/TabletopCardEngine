using FluentAssertions;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// The public schema entry point over the test host's plain <c>UseSqlite</c>, which is exactly the
/// configuration that has no migrations to apply.
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
        using var host = TestEngineHost.Setup(out _, out _);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var thrown = await manager.Invoking(schemaManager => schemaManager.MigrateAsync())
            .Should().ThrowExactlyAsync<HoldemSchemaException>();

        thrown.Which.Failure.Should().Be(HoldemSchemaFailure.MigrationsNotFound);
        thrown.Which.Message.Should().Contain("UseHoldemSqlite");
    }

    [TestMethod]
    public async Task SchemaManager_Status_OnAnEnsureCreatedDatabase()
    {
        using var host = TestEngineHost.Setup(out _, out _);
        var manager = host.GetRequiredService<IHoldemSchemaManager>();

        var status = await manager.GetSchemaStatusAsync();

        status.State.Should().Be(HoldemSchemaState.UpToDate);
        status.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        status.PendingMigrations.Should().BeEmpty();
        status.EngineSchemaVersion.Should().Be(1);
        status.DatabaseSchemaVersion.Should().BeNull();
        status.DatabaseEngineVersion.Should().BeNull();
        status.Should().Be(await manager.GetSchemaStatusAsync(), "two reads of the same database compare equal");
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
}
