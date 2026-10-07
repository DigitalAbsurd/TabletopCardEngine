namespace TabletopCardEngine;

/// <summary>
/// How a database's schema compares with the one the engine expects. The numeric values are part of
/// the contract: explicit, never renumbered and never reused. There is deliberately no zero member,
/// so <c>default</c> is never a valid state.
/// </summary>
public enum HoldemSchemaState
{
    /// <summary>Every migration the engine knows of has been applied.</summary>
    UpToDate = 1,

    /// <summary>The engine has migrations the database has not applied yet; call <see cref="IHoldemSchemaManager.MigrateAsync"/>.</summary>
    MigrationRequired = 2,

    /// <summary>The database was migrated by a newer engine than this one, which must not write to it.</summary>
    NewerThanEngine = 3,

    /// <summary>The provider keeps no schema history (InMemory), so there is no version to compare.</summary>
    NotVersioned = 4,
}
