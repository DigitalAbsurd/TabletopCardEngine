namespace OpenHoldemEngine;

/// <summary>
/// Machine-readable reason a <see cref="HoldemSchemaException"/> was thrown. The numeric values are
/// part of the contract: explicit, never renumbered and never reused. There is deliberately no zero
/// member, so <c>default</c> is never a valid failure.
/// </summary>
public enum HoldemSchemaFailure
{
    /// <summary>The configured EF Core provider is not one of the four the engine supports.</summary>
    UnsupportedProvider = 1,

    /// <summary>
    /// The provider is relational but no migrations were found, because it was configured with the
    /// plain EF Core call instead of the <c>UseHoldem*</c> call of the provider's engine package.
    /// </summary>
    MigrationsNotFound = 2,

    /// <summary>The database was migrated by a newer engine than this one.</summary>
    DatabaseIsNewer = 3,

    /// <summary>The database's schema is behind the engine's and has to be migrated first.</summary>
    MigrationRequired = 4,
}
