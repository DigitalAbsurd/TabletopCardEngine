using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Holdem.Options;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>Checks a new game's options against the v1 scope and the container's seat limits.</summary>
internal static class GameOptionsValidation
{
    internal static HoldemError? Validate(GameOptionData options, HoldemSettings settings)
    {
        if (!SupportedGameVariants.IsSupported(options.GameType, options.BettingStructure))
        {
            return new HoldemError(HoldemErrorCode.UnsupportedGameOptions, PackResource.HoldemValidation_UnsupportedGameOptions);
        }

        if (!Enum.IsDefined(options.TableFormat)
            || options.MaxPlayersCount < options.TableFormat.MinSeats()
            || options.MaxPlayersCount > options.TableFormat.MaxSeats()
            || options.MaxPlayersCount < settings.GamePlayersMinCount
            || options.MaxPlayersCount > settings.GamePlayersMaxCount
            || options.InitialSmallBlind <= 0
            || !AmountRules.HasValidScale(options.InitialSmallBlind)
            || options.InitialDealerSeatNo < 1
            || options.InitialDealerSeatNo > options.MaxPlayersCount
            || !AreBuyInOptionsValid(options)
            || !IsBlindScheduleValid(options))
        {
            return new HoldemError(HoldemErrorCode.InvalidGameOptions, PackResource.HoldemValidation_InvalidGameOptions);
        }

        if (options.RaiseCap < 1)
        {
            return new HoldemError(HoldemErrorCode.InvalidGameOptions, PackResource.HoldemValidation_RaiseCapBelowOne);
        }

        return null;
    }

    private static bool AreBuyInOptionsValid(GameOptionData options)
        => IsValidBuyInLimit(options.MinBuyIn)
            && IsValidBuyInLimit(options.MaxBuyIn)
            && !(options.MinBuyIn > options.MaxBuyIn)
            && !(options.MaxRebuys < 0)
            && (options.MaxRebuys is null || options.AllowRebuy);

    private static bool IsBlindScheduleValid(GameOptionData options)
    {
        var schedule = options.BlindSchedule;
        if (schedule is null)
        {
            return true;
        }

        var thresholds = schedule.PlayerThresholds;
        var levels = schedule.SmallBlindLevels;
        if (thresholds is null
            || levels is null
            || !Enum.IsDefined(schedule.Mode)
            || (schedule.IncreaseEveryHands is null && thresholds.Count == 0 && !schedule.IncreaseOnElimination)
            || schedule.IncreaseEveryHands < 1
            || thresholds.Any(value => value < 2 || value > options.MaxPlayersCount - 1)
            || thresholds.Distinct().Count() != thresholds.Count
            || BlindScheduleJson.Serialize(schedule)!.Length > BlindScheduleJson.MaxLength)
        {
            return false;
        }

        return schedule.Mode == BlindIncreaseMode.Multiplier
            ? levels.Count == 0 && IsValidMultiplierSchedule(schedule, options.InitialSmallBlind)
            : IsValidLevelsSchedule(schedule, options.InitialSmallBlind);
    }

    private static bool IsValidMultiplierSchedule(BlindScheduleData schedule, decimal initialSmallBlind)
        => schedule.Multiplier > 1
            && AmountRules.HasValidScale(schedule.Multiplier)
            && (schedule.MaxSmallBlind is null
                || (schedule.MaxSmallBlind >= initialSmallBlind && AmountRules.HasValidScale(schedule.MaxSmallBlind.Value)));

    private static bool IsValidLevelsSchedule(BlindScheduleData schedule, decimal initialSmallBlind)
    {
        if (schedule.MaxSmallBlind is not null || schedule.SmallBlindLevels.Count == 0)
        {
            return false;
        }

        var previous = initialSmallBlind;
        foreach (var level in schedule.SmallBlindLevels)
        {
            if (level <= previous || !AmountRules.HasValidScale(level))
            {
                return false;
            }

            previous = level;
        }

        return true;
    }

    private static bool IsValidBuyInLimit(decimal? limit)
        => limit is null || (limit > 0 && AmountRules.HasValidScale(limit.Value));
}
