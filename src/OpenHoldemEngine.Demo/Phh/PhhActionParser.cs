using System.Globalization;

namespace OpenHoldemEngine.Demo.Phh;

/// <summary>
/// Parses the action grammar of the PHH format. See
/// <see href="https://phh.readthedocs.io/en/stable/spec.html">the specification</see>; the shapes are
/// <c>d dh p&lt;n&gt; cards</c>, <c>d db cards</c>, <c>p&lt;n&gt; f</c>, <c>p&lt;n&gt; cc</c>,
/// <c>p&lt;n&gt; cbr amount</c>, <c>p&lt;n&gt; sm [cards]</c> and <c>p&lt;n&gt; sd [cards]</c>,
/// each optionally followed by <c># commentary</c>.
///
/// This is a port of <c>OpenHoldemEngine.Tests.Phh.PhhActionParser</c> into the demo project.
/// See the related test project for the original definition and the possible future consolidation
/// into a shared <c>OpenHoldemEngine.Phh</c> project (see task 11 notes in docs/demo-tasks.md).
/// </summary>
public static class PhhActionParser
{
    public static PhhAction Parse(string rawAction)
    {
        var commentIx = rawAction.IndexOf('#');
        var tokens = (commentIx < 0 ? rawAction : rawAction[..commentIx])
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 2)
        {
            throw new FormatException($"'{rawAction}' is not a PHH action");
        }

        if (tokens[0] == "d")
        {
            return tokens[1] switch
            {
                "dh" when tokens.Length == 4 => new PhhDealHoleAction(rawAction, ParsePlayerIndex(tokens[2], rawAction), PhhCards.ParseMany(tokens[3])),
                "db" when tokens.Length == 3 => new PhhDealBoardAction(rawAction, PhhCards.ParseMany(tokens[2])),
                _ => throw new FormatException($"'{rawAction}' is not a recognised PHH dealing action")
            };
        }

        var playerIndex = ParsePlayerIndex(tokens[0], rawAction);

        return tokens[1] switch
        {
            "f" when tokens.Length == 2 => new PhhFoldAction(rawAction, playerIndex),
            "cc" when tokens.Length == 2 => new PhhCheckCallAction(rawAction, playerIndex),
            "cbr" when tokens.Length == 3 => new PhhCompleteBetRaiseAction(rawAction, playerIndex, ParseAmount(tokens[2], rawAction)),
            "sm" => new PhhShowMuckAction(rawAction, playerIndex, ParseOptionalCards(tokens)),
            "sd" => new PhhStandPatDiscardAction(rawAction, playerIndex, ParseOptionalCards(tokens)),
            _ => throw new FormatException($"'{rawAction}' is not a recognised PHH player action")
        };

        static Data.Card?[] ParseOptionalCards(string[] tokens)
            => tokens.Length > 2 ? PhhCards.ParseMany(tokens[2]) : [];
    }

    /// <summary>Turns the <c>p&lt;n&gt;</c> label into a zero-based index; PHH numbers players from 1.</summary>
    private static int ParsePlayerIndex(string token, string rawAction)
    {
        if (token.Length < 2 || token[0] != 'p' || !int.TryParse(token[1..], CultureInfo.InvariantCulture, out var playerNo) || playerNo < 1)
        {
            throw new FormatException($"'{token}' in '{rawAction}' is not a PHH player label");
        }

        return playerNo - 1;
    }

    private static decimal ParseAmount(string token, string rawAction)
        => decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : throw new FormatException($"'{token}' in '{rawAction}' is not an amount");
}
