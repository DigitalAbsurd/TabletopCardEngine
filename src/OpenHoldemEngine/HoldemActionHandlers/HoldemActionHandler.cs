using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal abstract class HoldemActionHandler<T> : IHoldemActionHandler
    where T : HoldemAction
{
    public Task HandleAsync(HoldemAction holdemAction, HoldemDbContext dbContext)
        => ImplementHandleAsync((T)holdemAction, dbContext);

    internal abstract Task ImplementHandleAsync(T holdemAction, HoldemDbContext dbContext);
}
