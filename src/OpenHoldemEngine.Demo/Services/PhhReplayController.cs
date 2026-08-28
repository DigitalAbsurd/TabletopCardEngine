using OpenHoldemEngine.Data;
using OpenHoldemEngine.Demo.Phh;
using OpenHoldemEngine.Dto;
using OpenHoldemEngine.Options;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// Manages the step-by-step replay of a PHH hand history through <see cref="DemoGameSession"/>.
/// Maps PHH player decisions to engine actions, validates actor continuity, and tracks final stack
/// outcomes.
/// </summary>
public sealed class PhhReplayController
{
    private readonly DemoGameSession _session;
    private readonly PhhCardShuffler _shuffler;

    private PhhHand? _currentHand;
    private PhhHandDeck? _currentDeck;
    private Guid _currentGameId;
    private int _handNo;
    private int _nextActionIndex;
    private PlayerJoinData[]? _joinSeats;
    private string? _replayError;

    /// <summary>Gets the currently loaded hand, or null if no hand is loaded.</summary>
    public PhhHand? CurrentHand => _currentHand;

    /// <summary>Gets the current replay error message, or null if no error.</summary>
    public string? ReplayError
    {
        get => _replayError;
        private set => _replayError = value;
    }

    /// <summary>Gets the number of actions already replayed (for progress).</summary>
    public int ActionsReplayed => _nextActionIndex;

    /// <summary>Gets the total number of player actions in the hand.</summary>
    public int TotalPlayerActions => _currentHand?.Actions.OfType<IPhhPlayerDecision>().Count() ?? 0;

    /// <summary>Gets whether the replay has completed (all actions done).</summary>
    public bool IsComplete => _nextActionIndex >= TotalPlayerActions;

    /// <summary>Gets whether the final stacks match the recorded <see cref="PhhHand.FinishingStacks"/>.</summary>
    public bool? FinalStacksMatch { get; private set; }

    /// <summary>Gets a user-facing message about the final stacks verification, if applicable.</summary>
    public string FinalStacksMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new replay controller.
    /// </summary>
    /// <param name="session">The demo game session to apply actions through.</param>
    /// <param name="shuffler">The card shuffler for predetermined cards.</param>
    public PhhReplayController(DemoGameSession session, PhhCardShuffler shuffler)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
    }

    /// <summary>
    /// Loads a hand and resets the replay to the beginning.
    /// </summary>
    /// <param name="hand">The PHH hand to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>true if the hand loaded successfully, false otherwise.</returns>
    public async Task<bool> LoadHandAsync(PhhHand hand, CancellationToken cancellationToken = default)
    {
        ReplayError = null;
        FinalStacksMatch = null;
        FinalStacksMessage = string.Empty;

        if (hand == null)
        {
            ReplayError = "No hand provided.";
            return false;
        }

        _currentHand = hand;
        _currentDeck = PhhHandDeck.Create(hand);
        _nextActionIndex = 0;

        // Create join seats in ascending seat order (required by engine)
        _joinSeats = Enumerable.Range(1, hand.PlayerCount)
            .Select(seatNo => new PlayerJoinData(
                Name: hand.Players[seatNo - 1],
                SeatNo: (byte)seatNo,
                BuyInAmount: hand.StartingStacks[seatNo - 1] ?? 0))
            .ToArray();

        // Create a new game with this hand
        _currentGameId = Guid.NewGuid();
        _handNo = 1;

        var gameOptions = new GameOptionData
        {
            MaxPlayersCount = (byte)hand.PlayerCount,
            InitialSmallBlind = hand.SmallBlind,
            InitialDealerSeatNo = (byte)hand.PlayerCount // Last player is on the button
        };

        // Register the deck with the shuffler
        _shuffler.RegisterHand(_currentGameId, _handNo, _currentDeck);

        // Set the shuffler on the engine options
        _ = new HoldemEngineOptionsBuilder().WithCardShuffler(_shuffler);

        // Create the game
        if (!await _session.CreateGameAsync(gameOptions, _currentGameId, cancellationToken))
        {
            var entries = _session.Journal.Entries;
            var lastEntry = entries.Count > 0 ? entries[entries.Count - 1] : null;
            var errorMsg = lastEntry?.ErrorMessages.Length > 0 ? lastEntry.ErrorMessages[0] : "Unknown error";
            ReplayError = $"Failed to create game: {errorMsg}";
            return false;
        }

        // Start the first hand
        if (!await _session.StartFirstHandAsync(_joinSeats, cancellationToken))
        {
            var entries = _session.Journal.Entries;
            var lastEntry = entries.Count > 0 ? entries[entries.Count - 1] : null;
            var errorMsg = lastEntry?.ErrorMessages.Length > 0 ? lastEntry.ErrorMessages[0] : "Unknown error";
            ReplayError = $"Failed to start hand: {errorMsg}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Replays the next action in the hand.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>true if the action replayed successfully, false otherwise.</returns>
    public async Task<bool> ReplayNextActionAsync(CancellationToken cancellationToken = default)
    {
        ReplayError = null;

        if (_currentHand == null)
        {
            ReplayError = "No hand is loaded.";
            return false;
        }

        if (IsComplete)
        {
            ReplayError = "All actions have been replayed.";
            return false;
        }

        // Get all player decisions in order
        var playerDecisions = _currentHand.Actions.OfType<IPhhPlayerDecision>().ToList();

        if (_nextActionIndex >= playerDecisions.Count)
        {
            ReplayError = "No more actions to replay.";
            return false;
        }

        var decision = playerDecisions[_nextActionIndex];

        // Validate the recorded actor against the engine's expected actor
        var expectedSeatNo = _session.CurrentHand?.NextPlayerSeatNo ?? 0;
        var decidingSeatNo = (byte)(decision.PlayerIndex + 1); // Convert from 0-based to 1-based

        if (expectedSeatNo != decidingSeatNo)
        {
            ReplayError = $"Actor mismatch: PHH action '{decision}' is by player {decision.PlayerIndex + 1} " +
                         $"(seat {decidingSeatNo}), but the engine expects seat {expectedSeatNo}.";
            return false;
        }

        // Map PHH decision to engine action
        var (actionType, size) = MapPhhDecisionToAction(decision);

        // Apply the action through the session
        if (!await _session.ApplyPlayerActionAsync(actionType, size, cancellationToken))
        {
            var entries = _session.Journal.Entries;
            var lastEntry = entries.Count > 0 ? entries[entries.Count - 1] : null;
            var errorMsg = lastEntry?.ErrorMessages.Length > 0 ? lastEntry.ErrorMessages[0] : "Unknown error";
            ReplayError = $"Engine rejected PHH action '{decision}': {errorMsg}";
            return false;
        }

        _nextActionIndex++;

        // If hand is complete, verify the finishing stacks
        if (IsComplete && _session.CurrentHand != null && _session.CurrentHand.NextPlayerSeatNo == 0)
        {
            VerifyFinishingStacks();
        }

        return true;
    }

    /// <summary>
    /// Resets the replay to the initial state. The hand remains loaded but no actions have been applied.
    /// </summary>
    public void Reset()
    {
        _nextActionIndex = 0;
        ReplayError = null;
        FinalStacksMatch = null;
        FinalStacksMessage = string.Empty;
        _session.Reset();
    }

    /// <summary>
    /// Clears the loaded hand and all state.
    /// </summary>
    public void Unload()
    {
        Reset();
        _currentHand = null;
        _currentDeck = null;
        _joinSeats = null;
        _shuffler.Clear();
    }

    /// <summary>
    /// Maps a PHH player decision to an engine action type and size.
    /// </summary>
    private (PlayerActionType actionType, decimal size) MapPhhDecisionToAction(IPhhPlayerDecision decision)
    {
        return decision switch
        {
            PhhFoldAction => (PlayerActionType.Fold, 0),
            PhhCheckCallAction checkCall => MapCheckCall(checkCall),
            PhhCompleteBetRaiseAction raise => (PlayerActionType.Raise, raise.Amount),
            _ => throw new InvalidOperationException($"Unknown PHH decision type: {decision.GetType().Name}")
        };
    }

    /// <summary>
    /// Maps a PHH check/call action, distinguishing between Check and Call based on the engine's
    /// current evaluations.
    /// </summary>
    private (PlayerActionType actionType, decimal size) MapCheckCall(PhhCheckCallAction _)
    {
        var evaluations = _session.CurrentHand?.Evaluations ?? [];
        var hasCheck = evaluations.Any(e => e.PlayerActionType == PlayerActionType.Check);

        return hasCheck
            ? (PlayerActionType.Check, 0)
            : (PlayerActionType.Call, 0);
    }

    /// <summary>
    /// Compares the engine's final stacks against the recorded finishing stacks, if available.
    /// </summary>
    private void VerifyFinishingStacks()
    {
        if (_currentHand?.FinishingStacks == null)
        {
            FinalStacksMatch = null;
            FinalStacksMessage = "No recorded finishing stacks to verify.";
            return;
        }

        var currentGame = _session.CurrentGame;
        if (currentGame?.HandData?.Seats == null || currentGame.HandData.Seats.Length == 0)
        {
            FinalStacksMessage = "No final seat data to verify.";
            return;
        }

        // Verify each seat's final stack
        var matches = true;
        var details = new System.Text.StringBuilder();

        for (int i = 0; i < _currentHand.FinishingStacks.Length; i++)
        {
            var recordedStack = _currentHand.FinishingStacks[i];
            var seatNo = (byte)(i + 1);
            var seatData = currentGame.HandData.Seats.FirstOrDefault(s => s.SeatNo == seatNo);

            if (seatData == null)
            {
                // Seat folded or was not involved
                continue;
            }

            var engineStack = seatData.Stack;

            if (Math.Abs(recordedStack - engineStack) > 0.01m)
            {
                matches = false;
                details.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Seat {seatNo}: recorded {recordedStack:F2}, engine {engineStack:F2}");
            }
        }

        FinalStacksMatch = matches;
        FinalStacksMessage = matches
            ? "Final stacks match perfectly!"
            : $"Stack mismatch detected:\n{details}";
    }
}
