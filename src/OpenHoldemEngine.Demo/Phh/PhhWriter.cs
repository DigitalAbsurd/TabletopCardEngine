using OpenHoldemEngine.Demo.Services;
using OpenHoldemEngine.Dto;
using System.Globalization;

namespace OpenHoldemEngine.Demo.Phh;

/// <summary>
/// Exports a played hand to PHH (Poker Hand History) format.
/// Reconstructs the action list from <see cref="EngineCallJournal"/> and game state.
///
/// This is new code specific to the demo, not a port from the test project.
/// </summary>
public static class PhhWriter
{
    /// <summary>
    /// Generates a PHH-formatted string for a single hand.
    /// </summary>
    /// <param name="game">The game data containing the hand to export.</param>
    /// <param name="journal">The call journal containing all actions taken in this hand.</param>
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

        return GenerateHandString(hand, game.Options, journal);
    }

    /// <summary>
    /// Generates a PHH-formatted string for a complete multi-hand session.
    /// Each hand is wrapped in a numbered [N] section.
    /// </summary>
    /// <param name="game">The game data containing all hands played.</param>
    /// <param name="journal">The call journal containing all actions.</param>
    /// <returns>A complete, spec-conformant PHH session as a string.</returns>
    public static string WriteSession(GameData game, EngineCallJournal journal)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(journal);

        var hand = game.HandData;
        if (hand == null || hand.No <= 0)
        {
            throw new InvalidOperationException("No hands have been played yet.");
        }

        var lines = new List<string>
        {
            "[1]",
        };

        var handContent = GenerateHandString(hand, game.Options, journal);
        lines.AddRange(handContent.Split(Environment.NewLine));

        return string.Join(Environment.NewLine, lines);
    }

    private static string GenerateHandString(HandData hand, GameOptionData options, EngineCallJournal journal)
    {
        var lines = new List<string>();

        // Metadata section
        lines.Add("[Game]");
        lines.Add($@"variant = ""NT""");
        var antes = string.Join(", ", Enumerable.Repeat("0", hand.Seats.Length));
        lines.Add($"antes = [{antes}]");
        var sb = options.InitialSmallBlind.ToString(CultureInfo.InvariantCulture);
        var bb = (options.InitialSmallBlind * 2).ToString(CultureInfo.InvariantCulture);
        lines.Add($"blinds_or_straddles = [{sb}, {bb}]");
        var minBet = options.InitialSmallBlind.ToString(CultureInfo.InvariantCulture);
        lines.Add($"min_bet = {minBet}");
        var stacks = string.Join(", ", hand.Seats.Select(s => s.StartHandStack.ToString(CultureInfo.InvariantCulture)));
        lines.Add($"starting_stacks = [{stacks}]");
        var players = string.Join(@", """, hand.Seats.Select(s => s.Name));
        lines.Add($@"players = [""{players}""]");

        // Actions section
        lines.Add("actions =");
        lines.Add("[");

        var actions = ExtractActions(hand, journal);
        foreach (var action in actions)
        {
            var escaped = EscapeForToml(action);
            lines.Add($@"    ""{escaped}""");
        }

        // Finishing stacks if hand is complete
        if (hand.Street == Street.Finished && hand.NextPlayerSeatNo == 0)
        {
            var finishingStacks = string.Join(", ", hand.Seats.Select(s => s.Stack.ToString(CultureInfo.InvariantCulture)));
            lines.Add($@"    ""finishing_stacks = [{finishingStacks}]""");
        }

        lines.Add("]");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Extracts the action sequence from the journal and game state, formatted as PHH directives.
    /// </summary>
    private static List<string> ExtractActions(HandData hand, EngineCallJournal journal)
    {
        var actions = new List<string>();
        var boardCardsEmitted = false;
        var seatToLastActionNoMap = new Dictionary<byte, int>();

        // Emit hole cards for all seated players
        foreach (var seat in hand.Seats)
        {
            if (seat.Hole != null && seat.Hole.Length > 0)
            {
                var playerIndex = Array.FindIndex(hand.Seats, s => s.SeatNo == seat.SeatNo);
                var holeCards = string.Join(" ", seat.Hole.Select(CardToPhh));
                actions.Add($"d dh p{playerIndex + 1} {holeCards}");
            }
        }

        // Walk through the journal and reconstruct player actions
        foreach (var entry in journal.Entries)
        {
            if (!entry.Succeeded)
            {
                continue;
            }

            if (entry.Action is HoldemActionStartGame or HoldemActionStartHand)
            {
                // Already handled hole cards above
                continue;
            }

            if (entry.Action is HoldemActionPlayerAction playerAction)
            {
                // Try to find which seat this action was for by checking seats' LastAction
                // This is a heuristic: we assume the action was taken by the seat whose LastAction.No is new
                byte? seatNo = null;

                foreach (var seat in hand.Seats)
                {
                    if (seat.LastAction != null)
                    {
                        if (!seatToLastActionNoMap.TryGetValue(seat.SeatNo, out var lastNo) ||
                            lastNo != seat.LastAction.No)
                        {
                            seatToLastActionNoMap[seat.SeatNo] = seat.LastAction.No;
                            seatNo = seat.SeatNo;
                            break;
                        }
                    }
                }

                // If we couldn't find the seat, skip this action (shouldn't happen in normal play)
                if (!seatNo.HasValue)
                {
                    continue;
                }

                // Emit board cards once before the first player action (if any board cards exist)
                if (!boardCardsEmitted && hand.CommunityCards.Length > 0)
                {
                    var boardStr = string.Join(" ", hand.CommunityCards.Select(CardToPhh));
                    actions.Add($"d db {boardStr}");
                    boardCardsEmitted = true;
                }

                // Find the player's index in the seats array
                var playerIndex = Array.FindIndex(hand.Seats, s => s.SeatNo == seatNo.Value);
                if (playerIndex >= 0)
                {
                    var actionStr = ActionToPhh(playerAction.ActionType, playerAction.Size, playerIndex);
                    actions.Add(actionStr);
                }
            }
        }

        return actions;
    }

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
    /// Formats a player action as a PHH directive.
    /// </summary>
    private static string ActionToPhh(PlayerActionType actionType, decimal size, int playerIndex)
    {
        return actionType switch
        {
            PlayerActionType.Fold => $"p{playerIndex + 1} f",
            PlayerActionType.Check => $"p{playerIndex + 1} cc",
            PlayerActionType.Call => $"p{playerIndex + 1} cc",
            PlayerActionType.Raise => $"p{playerIndex + 1} cbr {size.ToString(CultureInfo.InvariantCulture)}",
            _ => throw new ArgumentException($"Unknown action type: {actionType}"),
        };
    }

    /// <summary>
    /// Escapes special characters for TOML string values.
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
