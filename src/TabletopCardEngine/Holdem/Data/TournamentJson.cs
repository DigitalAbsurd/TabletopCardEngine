using System.Text.Json;

namespace TabletopCardEngine.Holdem.Data;

/// <summary>The one JSON form of a tournament's lists (<see cref="Tournament.Levels"/> and <see cref="Tournament.Payouts"/>), the way <see cref="BlindScheduleJson"/> is for a blind schedule.</summary>
internal static class TournamentJson
{
    /// <summary>The column's length: the same bound as <see cref="BlindScheduleJson.MaxLength"/>, for the same reason.</summary>
    internal const int MaxLength = BlindScheduleJson.MaxLength;

    internal static string Serialize<T>(List<T>? list)
        => JsonSerializer.Serialize(list ?? [], (JsonSerializerOptions?)null);

    internal static List<T> Deserialize<T>(string json)
        => JsonSerializer.Deserialize<List<T>>(json, (JsonSerializerOptions?)null) ?? [];
}
