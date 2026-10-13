using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Sqlite.Design;

/// <summary>
/// What <c>dotnet ef</c> builds the context with when it scaffolds this package's migrations. The
/// library has no host to read a configuration from, and <c>migrations add</c> only needs the model,
/// so the data source is never opened.
/// </summary>
internal sealed class SqliteDesignTimeContextFactory : IDesignTimeDbContextFactory<HoldemDbContext>
{
    public HoldemDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<HoldemDbContext>().UseHoldemSqlite("Data Source=design-time.db").Options);
}
