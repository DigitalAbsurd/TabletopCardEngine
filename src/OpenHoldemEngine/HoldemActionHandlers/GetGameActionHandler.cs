using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class GetGameActionHandler : HoldemActionHandler<HoldemActionGetGame>
{
    internal override Task ImplementHandleAsync(HoldemActionGetGame holdemAction, HoldemDbContext dbContext)
    {
        return Task.CompletedTask;
    }
}
