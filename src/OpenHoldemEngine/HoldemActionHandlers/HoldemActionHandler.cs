using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal abstract class HoldemActionHandler<T> : IHoldemActionHandler
    where T : HoldemAction
{
    public Task<Result> HandleAsync(HoldemAction holdemAction, Game game)
        => ImplementHandleAsync((T)holdemAction, game);

    internal abstract Task<Result> ImplementHandleAsync(T holdemAction, Game game);


}
