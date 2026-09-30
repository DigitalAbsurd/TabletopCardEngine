using Microsoft.Data.Sqlite;
using OpenHoldemEngine.Demo.Phh;
using OpenHoldemEngine.Sqlite;

namespace OpenHoldemEngine.Demo.Engine;

/// <summary>
/// Configures the HoldemEngine for the Blazor WebAssembly demo.
/// Establishes a kept-alive in-memory SQLite database that persists for the lifetime of the app,
/// and two engines over it: the play engine, which deals at random, and the
/// <see cref="DemoReplayEngine"/>, which deals what the <see cref="PhhCardShuffler"/> holds.
/// </summary>
internal static class DemoEngineHost
{
    /// <summary>
    /// Configures HoldemEngine services and database for the WebAssembly demo.
    /// The database connection is held open for the lifetime of the application to preserve
    /// the in-memory SQLite schema and data between DbContext instances. The schema itself is not
    /// created here: <c>Program.Main</c> runs <c>IHoldemSchemaManager.MigrateAsync</c> once the host is built.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The SQLite connection, which must be kept alive for the duration of the app.</returns>
    internal static SqliteConnection ConfigureHoldemEngine(IServiceCollection services)
    {
        var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        keepAliveConnection.Open();

        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseHoldemSqlite(keepAliveConnection),
            configure: _ => { }
        );

        // One shuffler for the whole tab: the replay controller registers decks with the same
        // instance the replay engine reads them from.
        services.AddSingleton<PhhCardShuffler>();
        services.AddSingleton(serviceProvider =>
            DemoReplayEngine.Create(keepAliveConnection, serviceProvider.GetRequiredService<PhhCardShuffler>()));

        // Register the connection as a singleton so it can be accessed directly if needed
        // (e.g., for querying sqlite_master in the diagnostics page).
        services.AddSingleton(keepAliveConnection);

        return keepAliveConnection;
    }
}
