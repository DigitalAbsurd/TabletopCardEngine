namespace OpenHoldemEngine.Data;

/// <summary>The schema this build of the engine expects, reported as <see cref="HoldemSchemaStatus.EngineSchemaVersion"/>.</summary>
internal static class HoldemSchema
{
    /// <summary>Raised whenever a migration changes the schema; never lowered.</summary>
    internal const int Version = 1;

    /// <summary>The engine release that introduced this schema version; bumped together with <see cref="Version"/>.</summary>
    internal const string EngineVersion = "1.0.0";

    /// <summary>
    /// The engine's own migration history table, shared by every provider package, so that it never
    /// collides with a host's EF context keeping the default <c>__EFMigrationsHistory</c> in the same database.
    /// </summary>
    internal const string MigrationsHistoryTable = "__OheMigrationsHistory";

    /// <summary>Compares database schema version against the engine's version to determine state.</summary>
    /// <param name="databaseVersion">The version from the database, or null if the database is fresh or has no row.</param>
    /// <param name="hasPendingMigrations">Whether there are pending migrations to apply.</param>
    internal static HoldemSchemaState Compare(int? databaseVersion, bool hasPendingMigrations)
    {
        if (databaseVersion > Version)
        {
            return HoldemSchemaState.NewerThanEngine;
        }

        if (databaseVersion == null || databaseVersion < Version || hasPendingMigrations)
        {
            return HoldemSchemaState.MigrationRequired;
        }

        return HoldemSchemaState.UpToDate;
    }
}
