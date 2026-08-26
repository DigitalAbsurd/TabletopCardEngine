using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Dto;

/// <summary>The fixed rules a game is created with; they never change once the game exists.</summary>
/// <param name="MaxPlayersCount">How many seats the table has.</param>
/// <param name="InitialSmallBlind">The small blind; the big blind is always twice this.</param>
/// <param name="InitialDealerSeatNo">Where the button sits for the first hand. Seats are 1-based.</param>
/// <param name="AllowRebuy">Whether a busted seat may buy back in. Declared but not yet honored by any handler.</param>
/// <param name="MaxHandsCount">Hand limit for the game, or <c>0</c> for unlimited.</param>
/// <param name="AutoIncrementsHands">How many hands to deal automatically after one finishes, or <c>0</c> to deal on request.</param>
public record GameOptionData(
    int MaxPlayersCount = 6,
    decimal InitialSmallBlind = 1,
    byte InitialDealerSeatNo = 1,
    bool AllowRebuy = false,
    int MaxHandsCount = 0,
    int AutoIncrementsHands = 0);

/// <summary>A player taking a seat, together with the stack they bring to it.</summary>
/// <param name="Name">Display name of the player.</param>
/// <param name="SeatNo">The seat to occupy. Seats are 1-based; <c>0</c> means "no seat".</param>
/// <param name="BuyInAmount">The stack the player sits down with.</param>
public record PlayerJoinData(string Name, byte SeatNo, decimal BuyInAmount);

/// <summary>Chips added to a seat that is already occupied.</summary>
/// <param name="SeatNo">The seat being topped up. Seats are 1-based.</param>
/// <param name="Amount">How many chips to add.</param>
public record BuyInData(byte SeatNo, decimal Amount);

/// <summary>
/// One action the seat to act may legally take, with its sizing window.
/// <para>
/// The two bounds are on different bases by design: for <see cref="PlayerActionType.Call"/> they are
/// an increment ("how much more to put in"), while for <see cref="PlayerActionType.Raise"/> they are
/// both absolute street totals ("raise the street wager up to this") — the same basis
/// <see cref="HoldemActionPlayerAction.Size"/> expects. A "call" button wants a top-up amount; a
/// "raise to" slider wants an absolute.
/// </para>
/// </summary>
/// <param name="PlayerActionType">The action this window describes.</param>
/// <param name="MinValue">Smallest legal amount, on the basis described above.</param>
/// <param name="MaxValue">Largest legal amount, on the basis described above.</param>
public record PlayerActionEvaluationData(PlayerActionType PlayerActionType, decimal MinValue = 0, decimal MaxValue = 0);

/// <summary>What a seat walked away from a finished hand with.</summary>
/// <param name="Net">Chips won minus chips wagered over the whole hand; negative for a loser.</param>
/// <param name="Strength">A comparable hand-strength value — only meaningful against another seat's value in the same hand.</param>
/// <param name="RankType">The display category of the made hand.</param>
/// <param name="Description">Human-readable description of the hand, when the evaluator supplied one.</param>
public record HandResultData(decimal Net, uint Strength, HandRankType RankType, string? Description = null);
