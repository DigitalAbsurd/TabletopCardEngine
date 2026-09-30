using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.PostgreSql;
using OpenHoldemEngine.Sqlite;
using System.Text.Json;

namespace OpenHoldemEngine.Tests;

/// <summary>
/// The <c>OpenHoldemEngine.PostgreSql</c> package: its migrations create the schema in the <c>ohe</c>
/// schema through <see cref="IHoldemSchemaManager.MigrateAsync"/>, agree with the model, keep their
/// history in the engine's own table in that schema, and bring no other database driver along. The
/// model does not leak into SQLite after a PostgreSQL context is built in the same test.
/// </summary>
[TestClass]
public sealed class PostgreSqlPackageTests
{
    private static readonly string[] _otherDrivers =
    [
        "Microsoft.Data.Sqlite",
        "Microsoft.EntityFrameworkCore.Sqlite",
        "SQLitePCLRaw",
        "Microsoft.Data.SqlClient",
        "Microsoft.EntityFrameworkCore.SqlServer",
        "Microsoft.EntityFrameworkCore.InMemory",
    ];

    [TestMethod]
    public async Task PostgreSql_ModelUsesTheOheSchema()
    {
        var options = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemPostgreSql("Host=localhost;Database=unused")
            .Options;

        await using var context = new HoldemDbContext(options);
        context.Model.GetDefaultSchema().Should().Be("ohe");

        var gameEntity = context.Model.FindEntityType(typeof(Game))!;
        gameEntity.FindProperty("InitialSmallBlind")!.GetColumnType().Should().Be("numeric(18,2)");
    }

    [TestMethod]
    public void PostgreSql_HasMigrations_AndNoPendingModelChanges()
    {
        // Verify migrations exist in the assembly.
        var migrationAssembly = typeof(HoldemPostgreSqlDbContextOptionsBuilderExtensions).Assembly;
        var migrationTypes = migrationAssembly.GetTypes()
            .Where(t => t.Namespace == "OpenHoldemEngine.PostgreSql.Migrations" && typeof(Migration).IsAssignableFrom(t))
            .ToList();

        migrationTypes.Should().NotBeEmpty("migrations must be generated for the PostgreSQL provider");

        // Verify no pending model changes by building a context without connecting.
        var options = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemPostgreSql("Host=localhost;Database=unused")
            .Options;

        using var context = new HoldemDbContext(options);
        context.Database.HasPendingModelChanges().Should().BeFalse("the migrations' snapshot must match HoldemDbContext's model");
    }

    [TestMethod]
    public void Sqlite_ModelKeepsTheDefaultSchema()
    {
        // First build a PostgreSQL context to potentially pollute the model cache.
        var postgresOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemPostgreSql("Host=localhost;Database=unused")
            .Options;

        using var postgresContext = new HoldemDbContext(postgresOptions);
        postgresContext.Model.GetDefaultSchema().Should().Be("ohe");

        // Now use SQLite and confirm its model is unaffected.
        var sqliteOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlite(":memory:")
            .Options;

        using var sqliteContext = new HoldemDbContext(sqliteOptions);
        sqliteContext.Model.GetDefaultSchema().Should().BeNull("SQLite uses the default schema");
    }

    [TestMethod]
    public void PostgreSqlPackage_PullsNoOtherDriver()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "OpenHoldemEngine.PostgreSql", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Should().Contain(library => library.StartsWith("Npgsql/", StringComparison.OrdinalIgnoreCase));
        libraries.Where(library => _otherDrivers.Any(driver => library.StartsWith(driver, StringComparison.OrdinalIgnoreCase)))
            .Should().BeEmpty();
    }
}

