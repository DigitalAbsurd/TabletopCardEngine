using TabletopCardEngine.Demo.Engine;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Centralised session state for the demo: the only place that calls <see cref="IHoldemTable.ApplyAsync"/>.
/// Caches the last successful game data and provides typed access to the current hand.
/// <para>
/// It holds two engines over one database and calls whichever is active: the play engine deals at
/// random, the replay engine deals a hand history's recorded cards. Switching engines is how a
/// replay starts and ends; nothing reconfigures an engine in place.
/// </para>
/// </summary>
public sealed class DemoGameSession
{
    private readonly IHoldemTable _playEngine;
    private readonly DemoReplayEngine? _replayEngine;
    private readonly EngineCallJournal _journal;

    /// <summary>Gets the current game identifier, or <see cref="Guid.Empty"/> if no game has been created.</summary>
    public Guid CurrentGameId { get; private set; } = Guid.Empty;

    /// <summary>Gets the cached last successful game data, or <c>null</c> if no call has succeeded yet.</summary>
    public GameData? CurrentGame { get; private set; }

    /// <summary>
    /// Gets the current hand data, or <c>null</c> if no hand has been dealt yet
    /// (i.e. between CreateGame and the first StartGame).
    /// </summary>
    public HandData? CurrentHand => CurrentGame?.HandData is { No: > 0 } handData ? handData : null;

    /// <summary>Gets the engine the next call goes to: the play engine unless a replay is running.</summary>
    public IHoldemTable ActiveEngine { get; private set; }

    /// <summary>Gets whether calls currently go to the replay engine.</summary>
    public bool IsReplaying => _replayEngine != null && ReferenceEquals(ActiveEngine, _replayEngine.Engine);

    /// <summary>Gets the engine call journal containing all calls made through this session.</summary>
    public EngineCallJournal Journal => _journal;

    /// <summary>Raised whenever the cached game state changes after a successful engine call.</summary>
    public event EventHandler<EventArgs>? StateChanged;

    /// <summary>Raised whenever an engine call fails, carrying the user-friendly error message.</summary>
    public event EventHandler<EngineErrorEventArgs>? EngineError;

    /// <summary>
    /// Creates a new demo game session.
    /// </summary>
    /// <param name="engine">The play engine, which deals at random.</param>
    /// <param name="replayEngine">
    /// The engine that deals a hand history's cards, or <c>null</c> for a session that never replays.
    /// </param>
    public DemoGameSession(IHoldemTable engine, DemoReplayEngine? replayEngine = null)
    {
        _playEngine = engine ?? throw new ArgumentNullException(nameof(engine));
        _replayEngine = replayEngine;
        ActiveEngine = _playEngine;
        _journal = new EngineCallJournal();
    }

    /// <summary>Sends every following call to the replay engine.</summary>
    /// <exception cref="InvalidOperationException">The session was created without a replay engine.</exception>
    public void UseReplayEngine()
    {
        ActiveEngine = _replayEngine?.Engine
            ?? throw new InvalidOperationException("This session was created without a replay engine.");
    }

    /// <summary>Sends every following call to the play engine, which deals at random.</summary>
    public void UsePlayEngine()
    {
        ActiveEngine = _playEngine;
    }

    /// <summary>
    /// Creates a new game.
    /// </summary>
    /// <param name="options">The game options (blinds, max players, etc.).</param>
    /// <param name="gameId">Optional: a specific game ID to use. If null, a new one is generated.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the game was created successfully, <c>false</c> on validation error.</returns>
    public async Task<bool> CreateGameAsync(
        GameOptionData options,
        Guid? gameId = null,
        CancellationToken cancellationToken = default)
    {
        var action = HoldemAction.CreateGame(options, gameId);
        return await ApplyActionInternalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Starts the first hand of the game with the given players.
    /// </summary>
    /// <param name="joinSeats">
    /// The seats to fill, in any order (the engine sorts them).
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the hand was started successfully, <c>false</c> on validation error.</returns>
    public async Task<bool> StartFirstHandAsync(
        PlayerJoinData[] joinSeats,
        CancellationToken cancellationToken = default)
    {
        if (CurrentGameId == Guid.Empty)
        {
            RaiseEngineError("No game has been created yet. Call CreateGameAsync first.");
            return false;
        }

        var action = new HoldemActionStartGame(CurrentGameId, joinSeats);
        return await ApplyActionInternalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Starts the next hand of the current game.
    /// </summary>
    /// <param name="joinSeats">
    /// Players who sit down before this hand is dealt, each into a free seat, or <c>null</c> for none.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the hand was started successfully, <c>false</c> on validation error.</returns>
    public async Task<bool> StartNextHandAsync(
        PlayerJoinData[]? joinSeats = null,
        CancellationToken cancellationToken = default)
    {
        if (CurrentGameId == Guid.Empty)
        {
            RaiseEngineError("No game has been created yet.");
            return false;
        }

        var action = HoldemAction.StartHand(CurrentGameId, joinSeats);
        return await ApplyActionInternalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Stands a player up between hands, which frees the seat for the next hand.
    /// </summary>
    /// <param name="seatNo">The seat of the player who leaves.</param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the player left, <c>false</c> on validation error.</returns>
    public async Task<bool> LeaveSeatAsync(byte seatNo, CancellationToken cancellationToken = default)
    {
        if (CurrentGameId == Guid.Empty)
        {
            RaiseEngineError("No game has been created yet.");
            return false;
        }

        var action = HoldemAction.LeaveSeat(CurrentGameId, seatNo);
        return await ApplyActionInternalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Applies a player action in the current hand.
    /// </summary>
    /// <param name="actionType">The type of action (fold, check, call, or raise).</param>
    /// <param name="size">
    /// For raises, the absolute street total to raise to.
    /// Ignored for other action types.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the database operation.</param>
    /// <returns><c>true</c> if the action was applied successfully, <c>false</c> on validation error.</returns>
    public async Task<bool> ApplyPlayerActionAsync(
        PlayerActionType actionType,
        decimal size = 0,
        CancellationToken cancellationToken = default)
    {
        if (CurrentGameId == Guid.Empty)
        {
            RaiseEngineError("No game has been created yet.");
            return false;
        }

        var action = HoldemAction.Play(CurrentGameId, actionType, size);
        return await ApplyActionInternalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Resets the session state and clears the call journal.
    /// </summary>
    public void Reset()
    {
        CurrentGameId = Guid.Empty;
        CurrentGame = null;
        _journal.Clear();
    }

    /// <summary>
    /// Internal method that applies an action to the engine and updates the cached state.
    /// Catches any thrown exceptions and converts them to engine error events.
    /// </summary>
    private async Task<bool> ApplyActionInternalAsync(
        HoldemAction action,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ActiveEngine.ApplyAsync(action, cancellationToken);

            if (result.IsSuccess)
            {
                CurrentGameId = result.Value.GameId;
                CurrentGame = result.Value;
                _journal.RecordSuccess(action, result);
                RaiseStateChanged();
                return true;
            }

            // Record failure in journal
            var errorMessages = result.Errors
                .Select(error => error.Message)
                .ToArray();
            _journal.RecordFailure(action, errorMessages, result);

            // Map engine error messages to user-friendly ones
            var userMessage = MapEngineErrorToUserMessage(errorMessages);
            RaiseEngineError(userMessage);
            return false;
        }
        catch (Exception ex)
        {
            // Catch engine or database exceptions and surface them as engine errors
            var errorMessage = $"Engine call failed: {ex.Message}";
            _journal.RecordFailure(action, [ex.Message]);
            RaiseEngineError(errorMessage);
            return false;
        }
    }

    /// <summary>
    /// Maps engine validation error messages to user-friendly messages.
    /// </summary>
    private static string MapEngineErrorToUserMessage(string[] errorMessages)
    {
        if (errorMessages.Length == 0)
        {
            return "An unknown error occurred.";
        }

        // The three known validation errors from PackResource.resx
        var error = errorMessages[0];

        return error switch
        {
            "The game with passed Id already exists " => "Game already exists. Choose a different game ID.",
            "The current hand is completed" => "The current hand is already complete. Start the next hand to continue.",
            "The previous hand is not completed" => "The previous hand has not completed yet. Wait for the hand to finish before starting the next one.",
            _ => error, // Return raw message if it doesn't match known patterns
        };
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseEngineError(string message)
    {
        EngineError?.Invoke(this, new EngineErrorEventArgs(message));
    }
}

/// <summary>Event arguments for engine errors.</summary>
public sealed class EngineErrorEventArgs : EventArgs
{
    /// <summary>The user-friendly error message.</summary>
    public string Message { get; }

    /// <summary>Creates a new engine error event args.</summary>
    public EngineErrorEventArgs(string message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
}
