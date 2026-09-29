using OpenHoldemEngine.Dto;
using System.Globalization;
using System.Text;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// Generates readable C# code snippets from engine call journal entries.
/// Used to display the exact calls that produced the current game state,
/// so visitors can copy them into their own projects.
/// </summary>
public sealed class EngineCallSnippetWriter
{
    /// <summary>Generates a C# snippet for a single engine call.</summary>
    /// <param name="entry">The journal entry to generate a snippet for.</param>
    /// <returns>A multi-line C# code block.</returns>
    public static string GenerateSnippet(EngineCallJournalEntry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine("var result = await engine.ApplyHoldemActionAsync(");
        sb.Append("    ");
        sb.AppendLine(GenerateActionConstruction(entry.Action, indent: 1));
        sb.Append(')');
        if (entry.Succeeded)
        {
            sb.AppendLine(";");

            // What the seat on the clock may do next. HandData is null until the first hand is dealt
            // (fact 3), and a failed call has no Value to read at all, so only a success gets the line.
            sb.AppendLine("var availableActions = result.Value.HandData?.Evaluations;");
        }
        else
        {
            sb.Append("; // Error: ");
            if (entry.ErrorMessages.Length > 0)
            {
                sb.AppendLine(entry.ErrorMessages[0]);
            }
            else
            {
                sb.AppendLine("Failed");
            }
        }

        return sb.ToString();
    }

    /// <summary>Generates a complete console project snippet for the entire session.</summary>
    /// <param name="journal">The engine call journal containing all calls made in this session.</param>
    /// <returns>A compilable C# console program.</returns>
    public static string GenerateWholeSessionSnippet(EngineCallJournal journal)
    {
        var sb = new StringBuilder();

        // Using directives
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using OpenHoldemEngine;");
        sb.AppendLine("using OpenHoldemEngine.Dto;");
        sb.AppendLine();

        // Main entry
        sb.AppendLine("var services = new ServiceCollection();");
        sb.AppendLine("services.AddHoldemEngine(");
        sb.AppendLine("    opts => opts.UseSqlite(\"DataSource=:memory:\"),");
        sb.AppendLine("    configure: null);");
        sb.AppendLine("var provider = services.BuildServiceProvider();");
        sb.AppendLine("var engine = provider.GetRequiredService<HoldemEngine>();");
        sb.AppendLine();

        // All the calls from the journal
        foreach (var entry in journal.Entries)
        {
            sb.AppendLine(GenerateSnippet(entry));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Generates a single-line summary of an engine call result.</summary>
    /// <param name="entry">The journal entry to summarize.</param>
    /// <returns>A short text describing the result.</returns>
    public static string GenerateResultSummary(EngineCallJournalEntry entry)
    {
        if (entry.Succeeded)
        {
            return "Success";
        }

        if (entry.ErrorMessages.Length > 0)
        {
            return $"Error: {entry.ErrorMessages[0]}";
        }

        return "Failed";
    }

    private static string GenerateActionConstruction(HoldemAction action, int indent)
    {
        return action switch
        {
            HoldemActionGetGame => GenerateGetGameAction((HoldemActionGetGame)action),
            HoldemActionCreateGame => GenerateCreateGameAction((HoldemActionCreateGame)action, indent),
            HoldemActionStartHand => GenerateStartHandAction((HoldemActionStartHand)action, indent),
            HoldemActionPlayerAction => GeneratePlayerActionAction((HoldemActionPlayerAction)action, indent),
            _ => $"new UnknownAction(GameId: {FormatGuid(action.GameId)})",
        };
    }

    private static string GenerateGetGameAction(HoldemActionGetGame action)
    {
        return $"new HoldemActionGetGame(GameId: {FormatGuid(action.GameId)})";
    }

    private static string GenerateCreateGameAction(HoldemActionCreateGame action, int indent)
    {
        var nextIndent = new string(' ', (indent + 1) * 4);
        var indent2 = new string(' ', (indent + 2) * 4);
        var sb = new StringBuilder();
        sb.AppendLine("new HoldemActionCreateGame(");
        sb.Append(nextIndent).Append("GameId: ").AppendLine(FormatGuid(action.GameId) + ",");
        sb.Append(nextIndent).AppendLine("Options: new GameOptionData(");
        sb.Append(indent2).Append("MaxPlayersCount: ").AppendLine(action.Options.MaxPlayersCount + ",");
        sb.Append(indent2).Append("InitialSmallBlind: ").AppendLine(FormatDecimal(action.Options.InitialSmallBlind) + ",");
        sb.Append(indent2).Append("InitialDealerSeatNo: ").AppendLine(action.Options.InitialDealerSeatNo + ",");
        sb.Append(indent2).Append("AllowRebuy: ").AppendLine(FormatBool(action.Options.AllowRebuy) + ",");
        sb.Append(indent2).Append("MaxHandsCount: ").AppendLine(action.Options.MaxHandsCount + ",");
        sb.Append(indent2).Append("AutoIncrementsHands: ").Append(action.Options.AutoIncrementsHands).AppendLine("))");
        return sb.ToString().TrimEnd();
    }

    private static string GenerateStartHandAction(HoldemActionStartHand action, int indent)
    {
        var nextIndent = new string(' ', (indent + 1) * 4);
        var sb = new StringBuilder();
        sb.AppendLine("new HoldemActionStartHand(");
        sb.Append(nextIndent).Append("GameId: ").Append(FormatGuid(action.GameId));

        if (action.JoinSeats.Length > 0)
        {
            sb.AppendLine(",");
            sb.Append(nextIndent).AppendLine("JoinSeats:");
            sb.Append(GeneratePlayerJoinDataArray(action.JoinSeats, indent + 2));
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string GeneratePlayerActionAction(HoldemActionPlayerAction action, int indent)
    {
        var nextIndent = new string(' ', (indent + 1) * 4);
        var sb = new StringBuilder();
        sb.AppendLine("new HoldemActionPlayerAction(");
        sb.Append(nextIndent).Append("GameId: ").AppendLine(FormatGuid(action.GameId) + ",");
        sb.Append(nextIndent).Append("ActionType: PlayerActionType.").AppendLine(action.ActionType + ",");
        sb.Append(nextIndent).Append("Size: ").Append(FormatDecimal(action.Size)).Append(')');
        return sb.ToString();
    }

    private static string GeneratePlayerJoinDataArray(PlayerJoinData[] seats, int indent)
    {
        var indentStr = new string(' ', indent * 4);
        var nextIndent = new string(' ', (indent + 1) * 4);
        var sb = new StringBuilder();
        sb.Append(indentStr).AppendLine("[");
        for (int i = 0; i < seats.Length; i++)
        {
            var seat = seats[i];
            sb.Append(nextIndent)
                .Append("new PlayerJoinData(Name: \"")
                .Append(EscapeString(seat.Name))
                .Append("\", SeatNo: ")
                .Append(seat.SeatNo)
                .Append(", BuyInAmount: ")
                .Append(FormatDecimal(seat.BuyInAmount))
                .Append(')');
            if (i < seats.Length - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.Append(indentStr).Append(']');
        return sb.ToString();
    }

    private static string FormatGuid(Guid guid) => $"Guid.Parse(\"{guid:D}\")";

    private static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture) + 'm';
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
