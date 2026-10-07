using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using System.Net;

namespace TabletopCardEngine.Server.Tests;

/// <summary>Drives the real <c>src/TabletopCardEngine.Server</c> application over HTTP, on a database of its own for each provider.</summary>
[TestClass]
public sealed class ServerEndToEndTests
{
    /// <summary>
    /// Runs <paramref name="test"/> against a server host over a fresh database of <paramref name="providerName"/>;
    /// a SQLite file is deleted afterwards, a server database dies with its container.
    /// </summary>
    private static async Task RunAsync(string providerName, bool migrateOnStartup, Func<HttpClient, Task> test)
    {
        var connectionString = ServerDatabases.ConnectionStringFor(Enum.Parse<HoldemServerProvider>(providerName));
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                // Not Development, so appsettings.Development.json cannot hide a missing setting.
                builder.UseEnvironment("Production");
                builder.UseSetting("HoldemServer:Provider", providerName);
                builder.UseSetting("ConnectionStrings:Holdem", connectionString);
                builder.UseSetting("HoldemEngine:MigrateDatabaseOnStartup", migrateOnStartup ? "true" : "false");
            });
            using var client = factory.CreateClient();
            await test(client);
        }
        finally
        {
            if (providerName == nameof(HoldemServerProvider.Sqlite))
            {
                // Release the pooled connections, or Windows keeps the file locked.
                SqliteConnection.ClearAllPools();
                var dbFile = new SqliteConnectionStringBuilder(connectionString).DataSource;
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(dbFile)!, Path.GetFileName(dbFile) + "*"))
                {
                    File.Delete(file);
                }
            }
        }
    }

    [TestMethod]
    [DataRow("Sqlite")]
    [DataRow("PostgreSql")]
    [DataRow("SqlServer")]
    public Task Server_PlaysAGameFromCreationToFinish(string providerName) => RunAsync(providerName, true, GameScenario.PlayFromCreationToFinishAsync);

    [TestMethod]
    [DataRow("Sqlite", "Microsoft.EntityFrameworkCore.Sqlite")]
    [DataRow("PostgreSql", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [DataRow("SqlServer", "Microsoft.EntityFrameworkCore.SqlServer")]
    public Task Server_Health_ReportsTheSchemaState(string providerName, string expectedEfProvider) => RunAsync(providerName, true, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await GameScenario.ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("Healthy");
        var data = body.GetProperty("entries").GetProperty("tabletopcardengine").GetProperty("data");
        data.GetProperty("state").GetString().Should().Be("UpToDate");
        data.GetProperty("providerName").GetString().Should().Be(expectedEfProvider);
    });

    [TestMethod]
    public Task Server_Health_WithoutMigration_IsUnhealthy() => RunAsync("Sqlite", false, async client =>
    {
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await GameScenario.ReadJsonAsync(response);
        body.GetProperty("entries").GetProperty("tabletopcardengine").GetProperty("data").GetProperty("state").GetString().Should().Be("MigrationRequired");
    });
}
