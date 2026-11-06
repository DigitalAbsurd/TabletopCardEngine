using TabletopCardEngine.Holdem.Mappers;

namespace TabletopCardEngine.Holdem.Dto;

/// <summary>
/// The rules by which a game's blinds rise, chosen when the game is created. A game without a schedule
/// keeps <see cref="GameOptionData.InitialSmallBlind"/> for every hand. The big blind is always twice the small blind.
/// <para>
/// The engine has no clock, so every trigger is a count of hands or players and a game stays deterministic.
/// A schedule needs at least one trigger: <see cref="IncreaseEveryHands"/>, a non-empty
/// <see cref="PlayerThresholds"/>, or <see cref="IncreaseOnElimination"/>.
/// </para>
/// </summary>
public sealed record BlindScheduleData
{
    /// <summary>
    /// Raise one level after this many hands at the current level; the count restarts after any increase.
    /// <see langword="null"/> (the default) switches this trigger off; otherwise it must be at least 1.
    /// </summary>
    public int? IncreaseEveryHands { get; init; }

    /// <summary>
    /// Raise one level the first time the players dealt into a hand fall to or below each value; each value
    /// fires once. Values must be distinct and lie in <c>2..MaxPlayersCount - 1</c>. Empty by default.
    /// </summary>
    public IReadOnlyList<int> PlayerThresholds { get; init; } = EquatableList<int>.Empty;

    /// <summary>
    /// Raise one level whenever fewer players are dealt into a hand than into the previous one: a bust, a
    /// leave or a kick. <see langword="false"/> by default.
    /// </summary>
    public bool IncreaseOnElimination { get; init; }

    /// <summary>How the next level's small blind is worked out. Defaults to <see cref="BlindIncreaseMode.Multiplier"/>; an undefined value is rejected when the game is created.</summary>
    public BlindIncreaseMode Mode { get; init; } = BlindIncreaseMode.Multiplier;

    /// <summary>
    /// <see cref="BlindIncreaseMode.Multiplier"/> mode: each level's small blind is the previous one times
    /// this, rounded to the cent. Must be above 1 and have at most two decimal places. Defaults to 2.
    /// </summary>
    public decimal Multiplier { get; init; } = 2;

    /// <summary>
    /// <see cref="BlindIncreaseMode.Multiplier"/> mode only: the small blind never goes above this. When set
    /// it must be at least <see cref="GameOptionData.InitialSmallBlind"/> and have at most two decimal
    /// places. <see langword="null"/> (the default) means no cap.
    /// </summary>
    public decimal? MaxSmallBlind { get; init; }

    /// <summary>
    /// <see cref="BlindIncreaseMode.Levels"/> mode only: the small blinds after
    /// <see cref="GameOptionData.InitialSmallBlind"/>, strictly increasing, the first above the initial one,
    /// each with at most two decimal places. Once exhausted the last one holds. Empty by default.
    /// </summary>
    public IReadOnlyList<decimal> SmallBlindLevels { get; init; } = EquatableList<decimal>.Empty;
}
