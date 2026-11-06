using System.Text.Json;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.Data;

/// <summary>The one JSON form of a blind schedule: the stored column and the length check on a new game's options share it.</summary>
internal static class BlindScheduleJson
{
    /// <summary>The column's length. SQL Server bounds a string column (see <c>ModelConventionTests</c>), and 4 000 is the largest bound that stays an <c>nvarchar(n)</c>.</summary>
    internal const int MaxLength = 4000;

    internal static string? Serialize(BlindScheduleData? schedule)
        => schedule is null ? null : JsonSerializer.Serialize(schedule, (JsonSerializerOptions?)null);

    internal static BlindScheduleData? Deserialize(string? json)
        => json is null ? null : JsonSerializer.Deserialize<BlindScheduleData>(json, (JsonSerializerOptions?)null);
}
