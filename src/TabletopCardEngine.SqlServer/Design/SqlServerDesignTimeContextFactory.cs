using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TabletopCardEngine.Data;

namespace TabletopCardEngine.SqlServer.Design;

/// <summary>
/// What <c>dotnet ef</c> builds the context with when it scaffolds this package's migrations. The
/// library has no host to read a configuration from, and <c>migrations add</c> only needs the model,
/// so the database is never opened.
/// </summary>
internal sealed class SqlServerDesignTimeContextFactory : IDesignTimeDbContextFactory<HoldemDbContext>
{
    public HoldemDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<HoldemDbContext>().UseHoldemSqlServer("Server=localhost;Database=tce_design;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
