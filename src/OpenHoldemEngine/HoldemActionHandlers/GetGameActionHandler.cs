using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal sealed class GetGameActionHandler : HoldemActionHandler<HoldemActionGetGame>
{
    internal override Task<HoldemError?> ImplementHandleAsync(HoldemActionGetGame holdemAction, Game game)
    {
        return Task.FromResult<HoldemError?>(null);
    }
}
