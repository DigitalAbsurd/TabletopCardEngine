using TabletopCardEngine.Data;
using TabletopCardEngine.Dto;
using TabletopCardEngine.Resources;

namespace TabletopCardEngine.HoldemActionHandlers;

/// <summary>Answers an action record the engine has no handler for, so that a caller's own subclass fails cleanly instead of throwing.</summary>
internal sealed class UnknownActionHandler : IHoldemActionHandler
{
    public Task<HoldemError?> HandleAsync(HoldemAction holdemAction, Game game, CancellationToken cancellationToken)
        => Task.FromResult<HoldemError?>(new HoldemError(HoldemErrorCode.InvalidAction, PackResource.HoldemValidation_InvalidAction));
}
