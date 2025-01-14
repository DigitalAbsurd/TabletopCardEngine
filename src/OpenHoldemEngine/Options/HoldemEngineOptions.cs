using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine.Options;
public class HoldemEngineOptions
{
    public IDbContextFactory<GameDbContext> DbContextFactory { get; internal set; } = null!;
}
