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
            || !AreBuyInOptionsValid(options))
        {
            return new HoldemError(HoldemErrorCode.InvalidGameOptions, PackResource.HoldemValidation_InvalidGameOptions);
        }

        return null;
    }

    private static bool AreBuyInOptionsValid(GameOptionData options)
        => IsValidBuyInLimit(options.MinBuyIn)
            && IsValidBuyInLimit(options.MaxBuyIn)
            && !(options.MinBuyIn > options.MaxBuyIn)
            && !(options.MaxRebuys < 0)
            && (options.MaxRebuys is null || options.AllowRebuy);

    private static bool IsValidBuyInLimit(decimal? limit)
        => limit is null || (limit > 0 && AmountRules.HasValidScale(limit.Value));
}
