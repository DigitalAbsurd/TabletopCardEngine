using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.Data;

namespace OpenHoldemEngine;

public class HoldemEngine(IDbContextFactory<GameDbContext> _gameDbContextFactory)
{
}