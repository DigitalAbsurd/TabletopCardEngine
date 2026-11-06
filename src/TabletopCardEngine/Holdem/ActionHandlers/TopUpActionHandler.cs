using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

/// <summary>
/// Adds chips to an occupied seat between hands. Nothing changes in the hand that just ended: the chips are written
/// as a <see cref="BuyIn"/> row for the next hand, and <c>StartHand</c> deals them. Whether the call is a top-up or a
/// rebuy is read off the stack the seat would start that hand with before this call, because the two are governed by
/// different options and only a rebuy is counted.
/// </summary>
internal sealed class TopUpActionHandler : HoldemActionHandler<HoldemActionTopUp>
{
    /// <summary>Stacks are decimal, so "no chips left" is a comparison against this, never against zero.</summary>
    private const decimal DecimalMinPrecision = 0.0001M;

    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionTopUp holdemAction, Game game, CancellationToken cancellationToken)
        => Task.FromResult(Handle(holdemAction, game));

    private static HoldemError? Handle(HoldemActionTopUp holdemAction, Game game)
    {
        var currentHand = game.GetCurrentHand();
        if (currentHand.No == 0)
        {
            return new HoldemError(HoldemErrorCode.GameNotStarted, PackResource.HoldemValidation_GameNotStarted);
        }

        if (!game.IsCurrentHandEnded())
        {
            return new HoldemError(HoldemErrorCode.PreviousHandNotEnded, PackResource.HoldemValidation_PreviousHandNotEnded);
        }

        var outOfRange = holdemAction.SeatNo < 1 || holdemAction.SeatNo > game.MaxPlayersCount;
        if (outOfRange || game.GetSeatLatestPlayer(holdemAction.SeatNo) is not { HandNoEnd: null } player)
        {
            return new HoldemError(HoldemErrorCode.InvalidSeat, PackResource.HoldemValidation_InvalidSeat);
        }

        var amount = holdemAction.Amount;
        if (amount <= 0 || !AmountRules.HasValidScale(amount))
        {
            return new HoldemError(HoldemErrorCode.InvalidBuyIn, PackResource.HoldemValidation_InvalidBuyIn);
        }

        var pending = game.GetSeatPendingBuyIn(holdemAction.SeatNo);
        var nextStartStack = currentHand.GetSeatFinishStack(holdemAction.SeatNo) + (pending?.Amount ?? 0);
        var isTopUp = nextStartStack >= DecimalMinPrecision;

        var error = isTopUp ? ValidateTopUp(game, nextStartStack, amount) : ValidateRebuy(game, player, amount);
        if (error is not null)
        {
            return error;
        }

        // A second call between the same two hands adds to the row, since the key is per seat per hand; it keeps the
        // kind of the first call, so a rebuy topped up before it is dealt still counts as one rebuy.
        if (pending is null)
        {
            game.BuyIns.Add(new BuyIn
            {
                SeatNo = holdemAction.SeatNo,
                HandNo = game.GetNextHandNo(),
                Amount = amount,
                Kind = isTopUp ? BuyInKind.TopUp : BuyInKind.Rebuy,
            });
        }
        else
        {
            pending.Amount += amount;
        }

        return null;
    }

    // No minimum: a top-up only adds to a stack that is already playing.
    private static HoldemError? ValidateTopUp(Game game, decimal nextStartStack, decimal amount)
    {
        if (!game.AllowTopUp)
        {
            return new HoldemError(HoldemErrorCode.TopUpNotAllowed, PackResource.HoldemValidation_TopUpNotAllowed);
        }

        if (game.MaxBuyIn is not null && nextStartStack + amount > game.MaxBuyIn)
        {
            return new HoldemError(HoldemErrorCode.InvalidBuyIn, PackResource.HoldemValidation_InvalidBuyIn);
        }

        return null;
    }

    private static HoldemError? ValidateRebuy(Game game, Player player, decimal amount)
    {
        if (!game.AllowRebuy || (game.MaxRebuys is not null && game.CountPlayerRebuys(player) >= game.MaxRebuys))
        {
            return new HoldemError(HoldemErrorCode.RebuyNotAllowed, PackResource.HoldemValidation_RebuyNotAllowed);
        }

        if ((game.MinBuyIn is not null && amount < game.MinBuyIn) || (game.MaxBuyIn is not null && amount > game.MaxBuyIn))
        {
            return new HoldemError(HoldemErrorCode.InvalidBuyIn, PackResource.HoldemValidation_InvalidBuyIn);
        }

        return null;
    }
}
