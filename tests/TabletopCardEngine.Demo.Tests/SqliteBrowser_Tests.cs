using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed class SqliteBrowser_Tests
{
    private static (SqliteConnection Connection, SqliteBrowser Browser) CreateTestBrowser()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddTabletopCardEngine(tce => tce.AddHoldem(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: _ => { }
        ));

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHoldemSchemaManager>().MigrateAsync().GetAwaiter().GetResult();

        var browser = new SqliteBrowser(connection);
        return (connection, browser);
    }

    [TestMethod]
    public void GetTableNames_ReturnsNonEmptyList()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var tableNames = browser.GetTableNames();

        // Assert
        tableNames.Should().NotBeEmpty();
        tableNames.Should().Contain("Game");
        tableNames.Should().Contain("Hand");
        tableNames.Should().Contain("HandAction");

        connection.Dispose();
    }

    [TestMethod]
    public void GetCreateTableStatement_EveryTable_ReturnsNonNull()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();

        // Act & Assert
        foreach (var tableName in tableNames)
        {
            var createStatement = browser.GetCreateTableStatement(tableName);
            createStatement.Should().NotBeNullOrEmpty($"Table {tableName} should have a CREATE TABLE statement");
            createStatement.Should().Contain("CREATE TABLE", $"CREATE statement for {tableName} should contain CREATE TABLE");
        }

        connection.Dispose();
    }

    [TestMethod]
    public void GetColumnInfo_EveryTable_ReturnsNonEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();

        // Act & Assert
        foreach (var tableName in tableNames)
        {
            var columns = browser.GetColumnInfo(tableName);
            columns.Should().NotBeEmpty($"Table {tableName} should have at least one column");
            foreach (var col in columns)
            {
                col.Name.Should().NotBeNullOrEmpty();
                col.Type.Should().NotBeNullOrEmpty();
            }
        }

        connection.Dispose();
    }

    [TestMethod]
    public void GetRowCount_EveryTable_ReturnsNonNegative()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();

        // Act & Assert
        foreach (var tableName in tableNames)
        {
            var rowCount = browser.GetRowCount(tableName);
            rowCount.Should().BeGreaterOrEqualTo(0, $"Table {tableName} should have a non-negative row count");
        }

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_EveryTable_Succeeds()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();

        // Act & Assert
        foreach (var tableName in tableNames)
        {
            var rows = browser.GetTableRows(tableName, limit: 10, offset: 0);
            rows.Should().NotBeNull($"Table {tableName} should return a non-null list");
            rows.Should().BeOfType<List<IReadOnlyDictionary<string, object?>>>();
        }

        connection.Dispose();
    }

    [TestMethod]
    public void GetCreateTableStatement_EmptyTableName_ReturnsNull()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetCreateTableStatement("");

        // Assert
        result.Should().BeNull();

        connection.Dispose();
    }

    [TestMethod]
    public void GetCreateTableStatement_WhitespaceTableName_ReturnsNull()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetCreateTableStatement("   ");

        // Assert
        result.Should().BeNull();

        connection.Dispose();
    }

    [TestMethod]
    public void GetColumnInfo_EmptyTableName_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetColumnInfo("");

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }

    [TestMethod]
    public void GetColumnInfo_WhitespaceTableName_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetColumnInfo("   ");

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }

    [TestMethod]
    public void GetRowCount_EmptyTableName_ReturnsZero()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetRowCount("");

        // Assert
        result.Should().Be(0);

        connection.Dispose();
    }

    [TestMethod]
    public void GetRowCount_WhitespaceTableName_ReturnsZero()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetRowCount("   ");

        // Assert
        result.Should().Be(0);

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_EmptyTableName_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetTableRows("");

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_WhitespaceTableName_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();

        // Act
        var result = browser.GetTableRows("   ");

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_WithPaging_ReturnsCorrectSubset()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();
        var firstTable = tableNames[0];

        // Act
        var allRows = browser.GetTableRows(firstTable, limit: 1000, offset: 0);
        var pagedRows = browser.GetTableRows(firstTable, limit: 2, offset: 0);

        // Assert
        if (allRows.Count > 0)
        {
            pagedRows.Count.Should().BeLessOrEqualTo(2);
        }

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_NegativeLimit_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();
        var firstTable = tableNames[0];

        // Act
        var result = browser.GetTableRows(firstTable, limit: -1, offset: 0);

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }

    [TestMethod]
    public void GetTableRows_ZeroLimit_ReturnsEmpty()
    {
        // Arrange
        var (connection, browser) = CreateTestBrowser();
        var tableNames = browser.GetTableNames();
        var firstTable = tableNames[0];

        // Act
        var result = browser.GetTableRows(firstTable, limit: 0, offset: 0);

        // Assert
        result.Should().BeEmpty();

        connection.Dispose();
    }
}
