using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal abstract class HoldemActionHandler<T> : IHoldemActionHandler
    where T : HoldemAction
{
    public Task HandleAsync(HoldemAction holdemAction, Game game)
        => ImplementHandleAsync((T)holdemAction, game);

    internal abstract Task ImplementHandleAsync(T holdemAction, Game game);


}
