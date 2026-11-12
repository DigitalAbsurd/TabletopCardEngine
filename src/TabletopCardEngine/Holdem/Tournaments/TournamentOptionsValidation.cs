using TabletopCardEngine.Holdem.ActionHandlers;
using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.Tournaments;

/// <summary>Checks a new tournament's options: the scope its tables share with cash games, then its own limits.</summary>
internal static class TournamentOptionsValidation
{
    /// <summary>The most levels a tournament may have.</summary>
    internal const int MaxLevels = 100;

    /// <summary>The shortest level a tournament may have.</summary>
    internal static readonly TimeSpan MinLevelDuration = TimeSpan.FromMinutes(1);

    internal static HoldemError? Validate(TournamentOptionData? options)
    {
        if (options is null)
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidTables);
        }

        if (!SupportedGameVariants.IsSupported(GameType.Holdem, options.BettingStructure))
        {
            return new HoldemError(HoldemErrorCode.UnsupportedGameOptions, PackResource.HoldemValidation_TournamentUnsupportedGameOptions);
        }

        if (!Enum.IsDefined(options.TableFormat)
            || options.MaxPlayersPerTable < options.TableFormat.MinSeats()
            || options.MaxPlayersPerTable > options.TableFormat.MaxSeats()
            || options.RaiseCap < 1
            || !Enum.IsDefined(options.AnteType))
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidTables);
        }

        if (options.StartingStack <= 0
            || !AmountRules.HasValidScale(options.StartingStack)
            || options.BuyIn < 0
            || !AmountRules.HasValidScale(options.BuyIn))
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidAmounts);
        }

        if (options.MinEntries < 2 || options.MaxEntries < options.MinEntries)
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidEntryLimits);
        }

        if (!AreLevelsValid(options.Levels))
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidLevels);
        }

        if (!ArePayoutsValid(options.Payouts, options.MinEntries))
        {
            return Invalid(PackResource.HoldemValidation_TournamentInvalidPayouts);
        }

        return null;
    }

    private static bool AreLevelsValid(IReadOnlyList<TournamentLevelData>? levels)
    {
        if (levels is null || levels.Count == 0 || levels.Count > MaxLevels)
        {
            return false;
        }

        decimal previousSmallBlind = 0;
        foreach (var level in levels)
        {
            if (level is null
                || level.SmallBlind <= 0
                || !AmountRules.HasValidScale(level.SmallBlind)
                || level.SmallBlind < previousSmallBlind
                || level.Ante < 0
                || !AmountRules.HasValidScale(level.Ante)
                || level.Duration < MinLevelDuration
                || level.Duration.Ticks % TimeSpan.TicksPerSecond != 0
                || level.Duration.TotalSeconds > int.MaxValue)
            {
                return false;
            }

            previousSmallBlind = level.SmallBlind;
        }

        return TournamentJson.Serialize(TournamentDataMapper.ToEntityLevels(levels)).Length <= TournamentJson.MaxLength;
    }

    private static bool ArePayoutsValid(IReadOnlyList<decimal>? payouts, int minEntries)
    {
        if (payouts is null || payouts.Count == 0 || payouts.Count > minEntries)
        {
            return false;
        }

        var previous = decimal.MaxValue;
        decimal sum = 0;
        foreach (var payout in payouts)
        {
            if (payout <= 0 || payout > 100 || !AmountRules.HasValidScale(payout) || payout > previous)
            {
                return false;
            }

            previous = payout;
            sum += payout;
        }

        // The column bounds the payouts as it does the levels, and a long list of tiny shares can reach it first.
        return sum == 100 && TournamentJson.Serialize(payouts.ToList()).Length <= TournamentJson.MaxLength;
    }

    private static HoldemError Invalid(string message)
        => new(HoldemErrorCode.InvalidTournamentOptions, message);
}
