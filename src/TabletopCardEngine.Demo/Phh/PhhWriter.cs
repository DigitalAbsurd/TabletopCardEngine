using System.Globalization;
using System.Text;
using TabletopCardEngine.Demo.Services;
using TabletopCardEngine.Holdem;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Demo.Phh;

/// <summary>
/// Exports played hands to PHH (Poker Hand History) format, the layout <see cref="PhhReader"/> reads:
/// one numbered <c>[N]</c> table per hand with the keys directly inside it.
/// Each hand is built from the engine's own ledger, <see cref="HandData.Actions"/>, so the actors and
/// their order are what the engine recorded, not a reconstruction.
///
/// This is new code specific to the demo, not a port from the test project.
/// </summary>
public static class PhhWriter
{
    private const char Newline = '\n';

    private const string PotLimitComment = "# Pot-limit Texas hold'em: 'PT' is a non-standard variant code, not one of the PHH specification.";

    /// <summary>
    /// Generates a PHH-formatted string for a single hand, as table <c>[1]</c>.
    /// </summary>
    /// <param name="game">The game data containing the hand to export.</param>
    /// <param name="journal">The call journal of the session; the hand itself comes from <paramref name="game"/>.</param>
    /// <returns>A complete, spec-conformant PHH hand as a string.</returns>
    public static string WriteHand(GameData game, EngineCallJournal journal)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(journal);

        var hand = game.HandData;
        if (hand == null || hand.No <= 0)
        {
            throw new InvalidOperationException("No hand has been dealt yet.");
        }

        return WriteTables([hand], game.Options);
    }

    /// <summary>
    /// Generates a PHH-formatted string for a multi-hand session.
    /// Each hand the journal's results (and the current game) can reach is written as its own numbered
    /// <c>[N]</c> table, in the order the hands were played.
    /// </summary>
    /// <param name="game">The game data containing the current hand.</param>
    /// <param name="journal">The call journal; each successful entry's result carries the hand as it stood then.</param>
    /// <returns>A complete, spec-conformant PHH session as a string.</returns>
    public static string WriteSession(GameData game, EngineCallJournal journal)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(journal);

        var current = game.HandData;
        if (current == null || current.No <= 0)
        {
            throw new InvalidOperationException("No hands have been played yet.");
        }

        // The latest result of a hand is the hand as it ended, so a later entry replaces an earlier one.
        var handsByNo = new SortedDictionary<int, HandData>();
        foreach (var entry in journal.Entries)
        {
            if (entry.Succeeded && entry.Result is { IsSuccess: true } result && result.Value.HandData is { No: > 0 } hand)
            {
                handsByNo[hand.No] = hand;
            }
        }

        handsByNo[current.No] = current;

        return WriteTables([.. handsByNo.Values], game.Options);
    }

    private static string WriteTables(IReadOnlyList<HandData> hands, GameOptionData options)
    {
        var text = new StringBuilder();
        for (var i = 0; i < hands.Count; i++)
        {
            if (i > 0)
            {
                text.Append(Newline);
            }

            if (options.BettingStructure == BettingStructure.PotLimit)
            {
                text.Append(PotLimitComment).Append(Newline);
            }

            text.Append('[').Append(i + 1).Append(']').Append(Newline);
            WriteHandTable(text, hands[i], options.BettingStructure);
        }

        return text.ToString();
    }

    private static void WriteHandTable(StringBuilder text, HandData hand, BettingStructure structure)
    {
        var players = PlayersInOrder(hand);
        var smallBlind = hand.SmallBlind;
        var bigBlind = hand.BigBlind;

        text.Append("variant = '").Append(PhhSupport.VariantFor(structure)).Append('\'').Append(Newline);
        text.Append("antes = ").Append(Array(AntesPosted(hand, players))).Append(Newline);
        text.Append("blinds_or_straddles = ").Append(Array(players.Select((_, i) => i switch { 0 => smallBlind, 1 => bigBlind, _ => 0m }))).Append(Newline);
        if (structure == BettingStructure.FixedLimit)
        {
            // The specification forbids min_bet in a fixed-limit variant; the two bet sizes take its place.
            text.Append("small_bet = ").Append(Amount(bigBlind)).Append(Newline);
            text.Append("big_bet = ").Append(Amount(bigBlind * 2)).Append(Newline);
        }
        else
        {
            text.Append("min_bet = ").Append(Amount(bigBlind)).Append(Newline);
        }

        text.Append("starting_stacks = ").Append(Array(players.Select(seat => seat.StartHandStack))).Append(Newline);
        text.Append("players = [").Append(string.Join(", ", players.Select(seat => $"\"{EscapeForToml(seat.Name)}\""))).Append(']').Append(Newline);

        text.Append("actions = [").Append(Newline);
        foreach (var action in Actions(hand, players))
        {
            text.Append("    '").Append(action).Append("',").Append(Newline);
        }

        text.Append(']').Append(Newline);

        if (IsFinished(hand))
        {
            text.Append("finishing_stacks = ").Append(Array(players.Select(FinishingStack))).Append(Newline);
        }
    }

    /// <summary>
    /// What each player posted as an ante, from the ledger's <see cref="HandActionType.Ante"/> rows, in PHH
    /// player order. Heads-up the two entries are swapped, the way <c>blinds_or_straddles</c> is indexed
    /// (the small blind first, and that is the last player), so a reader maps them back by the same rule.
    /// </summary>
    private static List<decimal> AntesPosted(HandData hand, List<SeatData> players)
    {
        var antes = players
            .Select(seat => hand.Actions.Where(action => action.ActionType == HandActionType.Ante && action.SeatNo == seat.SeatNo).Sum(action => action.Amount))
            .ToList();

        if (antes.Count == 2)
        {
            antes.Reverse();
        }

        return antes;
    }

    /// <summary>
    /// The seats dealt into the hand in PHH order, which is how <see cref="PhhReplayController"/> seats
    /// them: <c>p1</c> is the first dealt-in seat after the button and the button is the last player.
    /// </summary>
    private static List<SeatData> PlayersInOrder(HandData hand)
    {
        var dealtIn = hand.Seats
            .Where(seat => seat.Hole is { Count: > 0 })
            .OrderBy(seat => seat.SeatNo)
            .ToList();

        return [.. dealtIn.Where(seat => seat.SeatNo > hand.DealerSeatNo), .. dealtIn.Where(seat => seat.SeatNo <= hand.DealerSeatNo)];
    }

    private static List<string> Actions(HandData hand, List<SeatData> players)
    {
        var actions = new List<string>();
        var playerBySeat = players
            .Select((seat, index) => (seat.SeatNo, Label: $"p{index + 1}"))
            .ToDictionary(player => player.SeatNo, player => player.Label);

        foreach (var seat in players)
        {
            actions.Add($"d dh {playerBySeat[seat.SeatNo]} {string.Concat(seat.Hole!.Select(CardToPhh))}");
        }

        var boardDealt = 0;
        var street = Street.PreFlop;
        var standingWager = 0m;

        foreach (var action in hand.Actions.OrderBy(action => action.No))
        {
            if (action.ActionType == HandActionType.Ante)
            {
                continue;
            }

            if (action.ActionType is HandActionType.SmallBlind or HandActionType.BigBlind)
            {
                standingWager = Math.Max(standingWager, action.Amount);
                continue;
            }

            if (action.Street != street)
            {
                street = action.Street;
                standingWager = 0m;
            }

            boardDealt = DealBoard(actions, hand, boardDealt, Math.Min(BoardSize(street), hand.CommunityCards.Count));

            var player = playerBySeat[action.SeatNo];
            switch (action.ActionType)
            {
                case HandActionType.Fold:
                    actions.Add($"{player} f");
                    break;
                case HandActionType.Check:
                case HandActionType.Call:
                    actions.Add($"{player} cc");
                    break;
                case HandActionType.Bet:
                case HandActionType.Raise:
                case HandActionType.AllIn:
                    // An all-in for no more than the standing wager is a call; PHH has no all-in action.
                    actions.Add(action.ActionType == HandActionType.AllIn && action.Amount <= standingWager
                        ? $"{player} cc"
                        : $"{player} cbr {Amount(action.Amount)}");
                    standingWager = Math.Max(standingWager, action.Amount);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown action type: {action.ActionType}");
            }
        }

        if (IsFinished(hand))
        {
            // Cards still to come when everyone is all in.
            DealBoard(actions, hand, boardDealt, hand.CommunityCards.Count);

            var contenders = players.Where(seat => !seat.IsFolded).ToList();
            if (contenders.Count >= 2)
            {
                foreach (var seat in contenders)
                {
                    actions.Add($"{playerBySeat[seat.SeatNo]} sm {string.Concat(seat.Hole!.Select(CardToPhh))}");
                }
            }
        }

        return actions;
    }

    /// <summary>Emits the board cards from <paramref name="dealt"/> up to <paramref name="target"/>, one deal per street.</summary>
    private static int DealBoard(List<string> actions, HandData hand, int dealt, int target)
    {
        while (dealt < target)
        {
            var count = dealt == 0 ? 3 : 1;
            count = Math.Min(count, target - dealt);
            actions.Add($"d db {string.Concat(hand.CommunityCards.Skip(dealt).Take(count).Select(CardToPhh))}");
            dealt += count;
        }

        return dealt;
    }

    /// <summary>Community cards on the table once <paramref name="street"/> is dealt; the street enum encodes the count.</summary>
    private static int BoardSize(Street street) => street switch
    {
        Street.Flop => 3,
        Street.Turn => 4,
        Street.River => 5,
        _ => 0,
    };

    private static bool IsFinished(HandData hand) => hand.Street == Street.Finished && hand.NextPlayerSeatNo == 0;

    private static decimal FinishingStack(SeatData seat)
        => seat.HandResult is null ? seat.Stack : seat.StartHandStack + seat.HandResult.Net;

    private static string Array(IEnumerable<decimal> amounts) => $"[{string.Join(", ", amounts.Select(Amount))}]";

    /// <summary>An amount without a database scale (<c>50</c>, not <c>50.0000</c>), in invariant culture.</summary>
    private static string Amount(decimal amount) => amount.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a card from engine format ("ah") to PHH format ("Ah").
    /// </summary>
    private static string CardToPhh(Card card)
    {
        var str = card.ToString();
        if (str.Length == 2)
        {
            return $"{char.ToUpper(str[0], CultureInfo.InvariantCulture)}{str[1]}";
        }

        return str;
    }

    /// <summary>
    /// Escapes special characters for TOML basic string values.
    /// </summary>
    private static string EscapeForToml(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }
}
