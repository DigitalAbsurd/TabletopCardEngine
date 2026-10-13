using TabletopCardEngine.Holdem.Data;
using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem.ActionHandlers;

internal sealed class GetGameActionHandler : HoldemActionHandler<HoldemActionGetGame>
{
    protected override bool AllowedWhenFinished => true;

    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionGetGame holdemAction, Game game, CancellationToken cancellationToken)
    {
        return Task.FromResult<HoldemError?>(null);
    }
}
