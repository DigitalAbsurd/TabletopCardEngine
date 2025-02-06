using FluentResults;
using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal sealed class GetGameActionHandler : HoldemActionHandler<HoldemActionGetGame>
{
    internal override Task<Result> ImplementHandleAsync(HoldemActionGetGame holdemAction, Game game)
    {
        return Task.FromResult(Result.Ok());
    }
}
