using System.Globalization;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;

namespace OpenHoldemEngine.Tests.Phh;

/// <summary>
/// Reads a bulk <c>.phhs</c> file: a TOML document whose top-level tables are numbered
/// (<c>[1]</c>, <c>[2]</c>, …), one per hand. That is the layout pokerkit's
/// <c>HandHistory.dump_all</c> writes, so the fixture stays readable by other PHH tooling.
/// </summary>
internal static partial class PhhReader
{
    internal static IReadOnlyList<PhhHand> ReadAll(string phhsText)
    {
        var sources = ReadSourceComments(phhsText);
        var document = Toml.ToModel(phhsText);

        return document
            .Where(entry => entry.Value is TomlTable && int.TryParse(entry.Key, CultureInfo.InvariantCulture, out _))
            .Select(entry => (Index: int.Parse(entry.Key, CultureInfo.InvariantCulture), Table: (TomlTable)entry.Value!))
            .OrderBy(entry => entry.Index)
            .Select(entry => ReadHand(entry.Table, entry.Index, sources.GetValueOrDefault(entry.Index, $"#{entry.Index}")))
            .ToArray();
    }

    /// <summary>
    /// The packer writes the upstream file name as a comment above each table. Tomlyn's object model
    /// drops comments, so they are recovered from the raw text instead.
    /// </summary>
    private static Dictionary<int, string> ReadSourceComments(string phhsText)
        => SourceCommentRegex().Matches(phhsText)
            .ToDictionary(
                match => int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture),
                match => match.Groups["source"].Value.Trim());

    [GeneratedRegex(@"^#[ \t]*(?<source>\S+)[ \t]*\r?\n\[(?<index>\d+)\]", RegexOptions.Multiline)]
    private static partial Regex SourceCommentRegex();

    private static PhhHand ReadHand(TomlTable table, int index, string source)
    {
        var startingStacks = ReadNullableDecimals(table, "starting_stacks", source);
        var players = ReadStrings(table, "players", source)
            // `players` is optional; fall back to the PHH player labels.
            ?? Enumerable.Range(1, startingStacks.Length).Select(no => $"p{no}").ToArray();

        return new PhhHand
        {
            Source = source,
            Index = index,
            Variant = ReadString(table, "variant", source),
            Antes = ReadDecimals(table, "antes", source),
            BlindsOrStraddles = ReadDecimals(table, "blinds_or_straddles", source),
            MinBet = ReadDecimal(table, "min_bet", source),
            StartingStacks = startingStacks,
            Players = players,
            FinishingStacks = table.ContainsKey("finishing_stacks") ? ReadDecimals(table, "finishing_stacks", source) : null,
            Actions = ReadStrings(table, "actions", source)!.Select(PhhActionParser.Parse).ToArray()
        };
    }

    private static string ReadString(TomlTable table, string key, string source)
        => table.TryGetValue(key, out var value) && value is string text
            ? text
            : throw new FormatException($"{source}: '{key}' is missing or is not a string");

    private static string[]? ReadStrings(TomlTable table, string key, string source)
        => table.TryGetValue(key, out var value)
            ? ReadArray(value, key, source).Select(item => item as string
                ?? throw new FormatException($"{source}: '{key}' contains a non-string element")).ToArray()
            : null;

    private static decimal ReadDecimal(TomlTable table, string key, string source)
        => table.TryGetValue(key, out var value)
            ? ToDecimal(value, key, source) ?? throw new FormatException($"{source}: '{key}' is null")
            : throw new FormatException($"{source}: '{key}' is missing");

    private static decimal[] ReadDecimals(TomlTable table, string key, string source)
        => ReadNullableDecimals(table, key, source)
            .Select(amount => amount ?? throw new FormatException($"{source}: '{key}' contains a null element"))
            .ToArray();

    private static decimal?[] ReadNullableDecimals(TomlTable table, string key, string source)
        => table.TryGetValue(key, out var value)
            ? ReadArray(value, key, source).Select(item => ToDecimal(item, key, source)).ToArray()
            : throw new FormatException($"{source}: '{key}' is missing");

    private static IEnumerable<object?> ReadArray(object? value, string key, string source)
        => value as TomlArray ?? throw new FormatException($"{source}: '{key}' is not an array");

    /// <summary>TOML integers arrive as <see cref="long"/> and floats as <see cref="double"/>.</summary>
    private static decimal? ToDecimal(object? value, string key, string source)
        => value switch
        {
            null => null,
            long integer => integer,
            double real => (decimal)real,
            _ => throw new FormatException($"{source}: '{key}' contains {value.GetType().Name}, expected a number")
        };
}
