using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Demo.Engine;

/// <summary>
/// Configures the HoldemEngine for the Blazor WebAssembly demo.
/// Establishes a kept-alive in-memory SQLite database that persists for the lifetime of the app.
/// </summary>
internal static class DemoEngineHost
{
    /// <summary>
    /// Configures HoldemEngine services and database for the WebAssembly demo.
    /// The database connection is held open for the lifetime of the application to preserve
    /// the in-memory SQLite schema and data between DbContext instances.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <returns>The SQLite connection, which must be kept alive for the duration of the app.</returns>
    internal static SqliteConnection ConfigureHoldemEngine(IServiceCollection services)
    {
        var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
        keepAliveConnection.Open();

        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseSqlite(keepAliveConnection),
            configure: _ => { }
        );

        // Register the connection as a singleton so it can be accessed directly if needed
        // (e.g., for querying sqlite_master in the diagnostics page).
        services.AddSingleton(keepAliveConnection);

        // Ensure database schema is created
        var serviceProvider = services.BuildServiceProvider();
        using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<HoldemDbContext>>().CreateDbContext();
        dbContext.Database.EnsureCreated();

        return keepAliveConnection;
    }
}
