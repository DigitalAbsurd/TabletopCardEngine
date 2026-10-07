using Microsoft.EntityFrameworkCore;

namespace TabletopCardEngine.Tests.Persistence.Upgrades;

/// <summary>
/// Seeds the schema as it was before one migration and checks the data survived that migration.
/// Every migration after the first needs exactly one, and <c>ProviderSchemaTests</c> fails when it has none.
/// </summary>
internal interface IUpgradeFixture
{
    /// <summary>The migration this fixture covers, e.g. <c>AddSomething</c>; matched against the suffix of the migration id.</summary>
    string MigrationName { get; }

    /// <summary>Writes rows with raw SQL valid at the migration before this one: the current model is the wrong schema here.</summary>
    Task SeedAsync(DbContext previousSchema, TestDatabaseKind kind);

    /// <summary>Checks, through a fully migrated engine, that the seeded data is intact.</summary>
    Task AssertAsync(IServiceProvider migratedEngine, TestDatabaseKind kind);
}
