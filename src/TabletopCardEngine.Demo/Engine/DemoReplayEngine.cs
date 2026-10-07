using Microsoft.Data.Sqlite;
using TabletopCardEngine.Demo.Phh;
using TabletopCardEngine.Sqlite;

namespace TabletopCardEngine.Demo.Engine;

/// <summary>
/// The engine that replays hand histories: a second <see cref="HoldemEngine"/>, built in a container
/// of its own with the <see cref="PhhCardShuffler"/> installed, over the same kept-alive connection
/// as the play engine.
/// <para>
/// Engine options belong to the container that registered them, so the only way to deal
/// pre-determined cards for a replay and random ones for a normal game is to have two engines and
/// pick one. Sharing the connection is what keeps a replayed game visible to the database inspector.
/// </para>
/// </summary>
public sealed class DemoReplayEngine : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    private DemoReplayEngine(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Engine = serviceProvider.GetRequiredService<HoldemEngine>();
    }

    /// <summary>The engine that deals the cards registered with the shuffler.</summary>
    public HoldemEngine Engine { get; }

    /// <summary>
    /// Builds the replay engine over an already open connection. The schema is not created here:
    /// <c>Program.Main</c> migrates the shared database once.
    /// </summary>
    /// <param name="connection">The kept-alive connection the play engine uses.</param>
    /// <param name="shuffler">The shuffler the replay controller registers each hand's deck with.</param>
    /// <returns>The replay engine, which owns its container.</returns>
    public static DemoReplayEngine Create(SqliteConnection connection, PhhCardShuffler shuffler)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(shuffler);

        var services = new ServiceCollection();
        services.AddHoldemEngine(
            dbOptionsAction: opts => opts.UseHoldemSqlite(connection),
            configure: opts => opts.WithCardShuffler(shuffler));

        return new DemoReplayEngine(services.BuildServiceProvider());
    }

    /// <inheritdoc/>
    public void Dispose() => _serviceProvider.Dispose();
}
