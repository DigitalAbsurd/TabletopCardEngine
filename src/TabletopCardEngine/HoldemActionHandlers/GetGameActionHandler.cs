using TabletopCardEngine.Data;
using TabletopCardEngine.Dto;

namespace TabletopCardEngine.HoldemActionHandlers;

internal sealed class GetGameActionHandler : HoldemActionHandler<HoldemActionGetGame>
{
    protected override bool AllowedWhenFinished => true;

    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionGetGame holdemAction, Game game, CancellationToken cancellationToken)
    {
        return Task.FromResult<HoldemError?>(null);
    }
}
