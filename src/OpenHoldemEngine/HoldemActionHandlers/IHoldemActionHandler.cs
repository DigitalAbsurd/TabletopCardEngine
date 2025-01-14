using OpenHoldemEngine.Data;
using OpenHoldemEngine.DTO;

namespace OpenHoldemEngine.HoldemActionHandlers;
internal interface IHoldemActionHandler
{
    Task HandleAsync(HoldemAction holdemAction, HoldemDbContext dbContext);
}
