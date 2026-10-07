namespace TabletopCardEngine.HoldemActionHandlers;

/// <summary>
/// The one table of supported <see cref="GameType"/> and <see cref="BettingStructure"/> combinations.
/// A new enum member is not playable until its rules exist and its pair is added here.
/// </summary>
internal static class SupportedGameVariants
{
    private static readonly (GameType GameType, BettingStructure BettingStructure)[] _supportedVariants =
    [
        (GameType.Holdem, BettingStructure.NoLimit),
    ];

    /// <summary>
    /// Tests whether the given game variant is supported.
    /// </summary>
    /// <param name="gameType">The game type to check.</param>
    /// <param name="bettingStructure">The betting structure to check.</param>
    /// <returns><c>true</c> if the pair is in the supported variants table; otherwise <c>false</c>.</returns>
    internal static bool IsSupported(GameType gameType, BettingStructure bettingStructure)
    {
        foreach (var (gt, bs) in _supportedVariants)
        {
            if (gt == gameType && bs == bettingStructure)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The complete collection of supported game variants.
    /// </summary>
    internal static IReadOnlyCollection<(GameType GameType, BettingStructure BettingStructure)> All => _supportedVariants;
}
