using Microsoft.Extensions.Logging;

namespace OpenHoldemEngine.Diagnostics;

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
internal static partial class HoldemEngineLog
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

    [LoggerMessage(EventId = 1020, Level = LogLevel.Debug, Message = "Hand {HandNo} of game {GameId} moved to {Street}")]
    internal static partial void StreetDealt(this ILogger logger, int handNo, Guid gameId, Street street);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Trace, Message = "Hand {HandNo} of game {GameId}: seat {SeatNo} was dealt {Cards}")]
    internal static partial void HoleCardsDealt(this ILogger logger, int handNo, Guid gameId, byte seatNo, IEnumerable<Card> cards);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Trace, Message = "Hand {HandNo} of game {GameId}: {Street} dealt {Cards}")]
    internal static partial void BoardDealt(this ILogger logger, int handNo, Guid gameId, Street street, IEnumerable<Card> cards);
}
