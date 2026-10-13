using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.AspNetCore.Tests;

[TestClass]
public sealed class MigrationOnStartupTests
{
    private static IHost BuildHost(SqliteConnection connection, bool migrateDatabaseOnStartup = false)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["TabletopCardEngine:Holdem:MigrateDatabaseOnStartup"] = migrateDatabaseOnStartup.ToString()
            });
        builder.Services.AddTabletopCardEngine(tce => tce.AddHoldem(
            builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
            db => db.UseHoldemSqlite(connection)));
        return builder.Build();
    }

    private static SqliteConnection OpenMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static async Task<HoldemSchemaState> GetSchemaStateAsync(IHost host)
    {
        var status = await host.Services.GetRequiredService<IHoldemSchemaManager>().GetSchemaStatusAsync();
        return status.State;
    }

    [TestMethod]
    public async Task MigrateDatabaseOnStartup_True_MigratesBeforeStartCompletes()
    {
        using var connection = OpenMemoryConnection();
        using var host = BuildHost(connection, migrateDatabaseOnStartup: true);

        await host.StartAsync();

        var state = await GetSchemaStateAsync(host);
        state.Should().Be(HoldemSchemaState.UpToDate);

        await host.StopAsync();
    }

    [TestMethod]
    public async Task MigrateDatabaseOnStartup_Absent_LeavesTheSchemaAlone()
    {
        using var connection = OpenMemoryConnection();
        using var host = BuildHost(connection, migrateDatabaseOnStartup: false);

        await host.StartAsync();

        var state = await GetSchemaStateAsync(host);
        state.Should().Be(HoldemSchemaState.MigrationRequired);

        await host.StopAsync();
    }

    [TestMethod]
    public async Task MigrateDatabaseOnStartup_True_RunsBeforeLaterHostedServices()
    {
        using var connection = OpenMemoryConnection();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["TabletopCardEngine:Holdem:MigrateDatabaseOnStartup"] = "true"
            });
        builder.Services.AddTabletopCardEngine(tce => tce.AddHoldem(
            builder.Configuration.GetSection("TabletopCardEngine:Holdem"),
            db => db.UseHoldemSqlite(connection)));

        HoldemSchemaState? probeState = null;
        builder.Services.AddHostedService<ProbeHostedService>(sp =>
            new ProbeHostedService(sp.GetRequiredService<IHoldemSchemaManager>(), state => probeState = state));

        using var host = builder.Build();
        await host.StartAsync();

        probeState.Should().Be(HoldemSchemaState.UpToDate);

        await host.StopAsync();
    }

    private sealed class ProbeHostedService : IHostedService
    {
        private readonly IHoldemSchemaManager _schemaManager;
        private readonly Action<HoldemSchemaState> _recordState;

        public ProbeHostedService(IHoldemSchemaManager schemaManager, Action<HoldemSchemaState> recordState)
        {
            _schemaManager = schemaManager;
            _recordState = recordState;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var status = await _schemaManager.GetSchemaStatusAsync(cancellationToken);
            _recordState(status.State);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
