using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;
using TabletopCardEngine.Data;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.PostgreSql;
using TabletopCardEngine.Sqlite;
using TabletopCardEngine.SqlServer;

namespace TabletopCardEngine.Tests;

/// <summary>
/// The <c>TabletopCardEngine.SqlServer</c> package: its migrations create the schema in the <c>tce</c>
/// schema through <see cref="IHoldemSchemaManager.MigrateAsync"/>, agree with the model, keep their
/// history in the engine's own table in that schema, and bring no other database driver along. The
/// model does not leak into SQLite or PostgreSQL after a SQL Server context is built in the same test.
/// </summary>
[TestClass]
public sealed class SqlServerPackageTests
{
    private static readonly string[] _otherDrivers =
    [
        "Microsoft.Data.Sqlite",
        "Microsoft.EntityFrameworkCore.Sqlite",
        "SQLitePCLRaw",
        "Npgsql",
        "Microsoft.EntityFrameworkCore.InMemory",
    ];

    [TestMethod]
    public async Task SqlServer_ModelUsesTheTceSchema()
    {
        var options = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlServer("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        await using var context = new HoldemDbContext(options);
        context.Model.GetDefaultSchema().Should().Be("tce");

        var gameEntity = context.Model.FindEntityType(typeof(Game))!;
        gameEntity.FindProperty("InitialSmallBlind")!.GetColumnType().Should().Be("decimal(18,2)");
    }

    [TestMethod]
    public void SqlServer_StringColumnsAreBounded()
    {
        var options = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlServer("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new HoldemDbContext(options);
        var playerEntity = context.Model.FindEntityType(typeof(Player))!;
        playerEntity.FindProperty("Name")!.GetColumnType().Should().Be("nvarchar(64)");

        var allColumns = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Select(p => p.GetColumnType())
            .ToList();

        allColumns.Should().NotContain("nvarchar(max)", "SQL Server unbounded string columns violate #200");
    }

    [TestMethod]
    public void SqlServer_HasMigrations_AndNoPendingModelChanges()
    {
        var migrationAssembly = typeof(HoldemSqlServerDbContextOptionsBuilderExtensions).Assembly;
        var migrationTypes = migrationAssembly.GetTypes()
            .Where(t => t.Namespace == "TabletopCardEngine.SqlServer.Migrations" && typeof(Migration).IsAssignableFrom(t))
            .ToList();

        migrationTypes.Should().NotBeEmpty("migrations must be generated for the SQL Server provider");

        var options = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlServer("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new HoldemDbContext(options);
        context.Database.HasPendingModelChanges().Should().BeFalse("the migrations' snapshot must match HoldemDbContext's model");
    }

    [TestMethod]
    public void Sqlite_ModelKeepsTheDefaultSchemaAfterSqlServer()
    {
        var sqlServerOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlServer("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var sqlServerContext = new HoldemDbContext(sqlServerOptions);
        sqlServerContext.Model.GetDefaultSchema().Should().Be("tce");

        var sqliteOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlite(":memory:")
            .Options;

        using var sqliteContext = new HoldemDbContext(sqliteOptions);
        sqliteContext.Model.GetDefaultSchema().Should().BeNull("SQLite uses the default schema");
    }

    [TestMethod]
    public void PostgreSql_ModelKeepsTheTceSchemaAfterSqlServer()
    {
        var sqlServerOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemSqlServer("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var sqlServerContext = new HoldemDbContext(sqlServerOptions);
        sqlServerContext.Model.GetDefaultSchema().Should().Be("tce");

        var postgresOptions = new DbContextOptionsBuilder<HoldemDbContext>()
            .UseHoldemPostgreSql("Host=localhost;Database=unused")
            .Options;

        using var postgresContext = new HoldemDbContext(postgresOptions);
        postgresContext.Model.GetDefaultSchema().Should().Be("tce");
    }

    [TestMethod]
    public void SqlServerPackage_PullsNoOtherDriver()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine.SqlServer", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Should().Contain(library => library.StartsWith("Microsoft.EntityFrameworkCore.SqlServer/", StringComparison.OrdinalIgnoreCase));
        libraries.Where(library => _otherDrivers.Any(driver => library.StartsWith(driver, StringComparison.OrdinalIgnoreCase)))
            .Should().BeEmpty();
    }

    [TestMethod]
    public void SqlitePackage_PullsNoSqlClient()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine.Sqlite", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Where(library => library.StartsWith("Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }

    [TestMethod]
    public void PostgreSqlPackage_PullsNoSqlClient()
    {
        var assetsFile = Path.Combine(RepositoryRoot.FullPath, "src", "TabletopCardEngine.PostgreSql", "obj", "project.assets.json");
        File.Exists(assetsFile).Should().BeTrue("the package must be restored before the tests run");

        using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToList();

        libraries.Where(library => library.StartsWith("Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }
}
