using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.Holdem.ActionHandlers;

internal abstract class HoldemActionHandler<T> : IHoldemActionHandler
    where T : HoldemAction
{
    /// <summary>Whether the action may run on a game that has been finished. Only reading the game back may.</summary>
    protected virtual bool AllowedWhenFinished => false;

    public Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game, CancellationToken cancellationToken)
    {
        if (game.IsFinished && !AllowedWhenFinished)
        {
            return Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.GameFinished, PackResource.HoldemValidation_GameFinished));
        }

        return ImplementHandleAsync((T)holdemAction, game, cancellationToken);
    }

    internal abstract Task<HoldemError?> ImplementHandleAsync(T holdemAction, Game game, CancellationToken cancellationToken);

}
