using Microsoft.Extensions.Logging;

namespace TabletopCardEngine.Holdem.Diagnostics;

/// <summary>
/// The engine's log events. Event IDs are part of the contract, so they are fixed here and never reused.
/// <para>
/// Levels follow one rule:
/// </para>
/// <list type="bullet">
/// <item><description><b>Warning</b>: a lost race.</description></item>
/// <item><description><b>Information</b>: a lifecycle change of a game or hand, and a rejected command (a caller-visible result).</description></item>
/// <item><description><b>Debug</b>: every applied command and every street change.</description></item>
/// <item><description><b>Trace</b>: reserved for the opt-in card events.</description></item>
/// </list>
/// <para>
/// The engine logs nothing at Error or Critical: exceptions propagate to the host, which logs them.
/// Only scalars are logged. A <c>GameData</c> carries every seat's hole cards and the player names, so it is never a log argument.
/// </para>
/// </summary>
internal static partial class HoldemLog
{
    /// <summary>Why a command failed with <see cref="HoldemErrorCode.Conflict"/>: the caller's expected version was stale.</summary>
    internal const string ExpectedVersion = nameof(ExpectedVersion);

    /// <summary>Why a command failed with <see cref="HoldemErrorCode.Conflict"/>: another writer saved first.</summary>
    internal const string ConcurrentSave = nameof(ConcurrentSave);

    private static readonly Func<ILogger, Guid, int, IDisposable?> _gameScope =
        LoggerMessage.DefineScope<Guid, int>("GameId:{GameId} HandNo:{HandNo}");

    /// <summary>Opens the scope that stamps every record of one call with the game and the hand it started on.</summary>
    internal static IDisposable? BeginGameScope(this ILogger logger, Guid gameId, int handNo)
        => _gameScope(logger, gameId, handNo);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "{ActionType} applied to game {GameId}, now at version {Version}")]
    internal static partial void ActionApplied(this ILogger logger, string actionType, Guid gameId, int version);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "{ActionType} on game {GameId} was rejected with {ErrorCode}")]
    internal static partial void ActionRejected(this ILogger logger, string actionType, Guid gameId, HoldemErrorCode errorCode);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "{ActionType} on game {GameId} failed with a concurrency conflict ({ConflictReason})")]
    internal static partial void ConcurrencyConflict(this ILogger logger, string actionType, Guid gameId, string conflictReason);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Game {GameId} created with {MaxPlayersCount} seats and a small blind of {SmallBlind}")]
    internal static partial void GameCreated(this ILogger logger, Guid gameId, int maxPlayersCount, decimal smallBlind);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "Hand {HandNo} of game {GameId} started with {SeatCount} seats")]
    internal static partial void HandStarted(this ILogger logger, int handNo, Guid gameId, int seatCount);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Hand {HandNo} of game {GameId} finished")]
    internal static partial void HandFinished(this ILogger logger, int handNo, Guid gameId);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Game {GameId} finished")]
    internal static partial void GameFinished(this ILogger logger, Guid gameId);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Information, Message = "Hand {HandNo} of game {GameId} raised the blinds to level {BlindLevel}: small blind {SmallBlind}")]
    internal static partial void BlindLevelRaised(this ILogger logger, int handNo, Guid gameId, int blindLevel, decimal smallBlind);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Debug, Message = "Hand {HandNo} of game {GameId} moved to {Street}")]
    internal static partial void StreetDealt(this ILogger logger, int handNo, Guid gameId, Street street);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "Tournament {TournamentId} created with {LevelCount} levels and a starting stack of {StartingStack}")]
    internal static partial void TournamentCreated(this ILogger logger, Guid tournamentId, int levelCount, decimal startingStack);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Entry {EntryNo} registered for tournament {TournamentId}, now {EntryCount} entries")]
    internal static partial void TournamentEntryRegistered(this ILogger logger, int entryNo, Guid tournamentId, int entryCount);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information, Message = "Entry {EntryNo} unregistered from tournament {TournamentId}, now {EntryCount} entries")]
    internal static partial void TournamentEntryUnregistered(this ILogger logger, int entryNo, Guid tournamentId, int entryCount);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information, Message = "Tournament {TournamentId} started with {EntryCount} entries at {TableCount} tables")]
    internal static partial void TournamentStarted(this ILogger logger, Guid tournamentId, int entryCount, int tableCount);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Debug, Message = "Table {TableNo} of tournament {TournamentId} dealt hand {HandNo} of game {GameId} at level {LevelIndex}: small blind {SmallBlind}")]
    internal static partial void TournamentLevelStarted(this ILogger logger, int tableNo, Guid tournamentId, int handNo, Guid gameId, int levelIndex, decimal smallBlind);

    [LoggerMessage(EventId = 1205, Level = LogLevel.Information, Message = "Entry {EntryNo} of tournament {TournamentId} eliminated in place {Place} with a prize of {Prize}, at table {TableNo} in hand {HandNo}")]
    internal static partial void TournamentEntryEliminated(this ILogger logger, int entryNo, Guid tournamentId, int place, decimal prize, int tableNo, int handNo);

    [LoggerMessage(EventId = 1206, Level = LogLevel.Information, Message = "Tournament {TournamentId} finished: entry {EntryNo} won it with a prize of {Prize}")]
    internal static partial void TournamentFinished(this ILogger logger, Guid tournamentId, int entryNo, decimal prize);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Trace, Message = "Hand {HandNo} of game {GameId}: seat {SeatNo} was dealt {Cards}")]
    internal static partial void HoleCardsDealt(this ILogger logger, int handNo, Guid gameId, byte seatNo, IEnumerable<Card> cards);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Trace, Message = "Hand {HandNo} of game {GameId}: {Street} dealt {Cards}")]
    internal static partial void BoardDealt(this ILogger logger, int handNo, Guid gameId, Street street, IEnumerable<Card> cards);
}
