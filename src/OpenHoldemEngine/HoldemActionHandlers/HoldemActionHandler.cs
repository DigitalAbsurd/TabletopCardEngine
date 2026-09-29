using OpenHoldemEngine.Data;
using OpenHoldemEngine.Dto;

namespace OpenHoldemEngine.HoldemActionHandlers;

internal abstract class HoldemActionHandler<T> : IHoldemActionHandler
    where T : HoldemAction
{
    public Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game)
        => ImplementHandleAsync((T)holdemAction, game);

    internal abstract Task<HoldemError?> ImplementHandleAsync(T holdemAction, Game game);

}
