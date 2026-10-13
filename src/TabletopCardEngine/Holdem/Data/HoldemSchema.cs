namespace TabletopCardEngine.Holdem.Data;

/// <summary>The schema this build of the engine expects, reported as <see cref="HoldemSchemaStatus.EngineSchemaVersion"/>.</summary>
internal static class HoldemSchema
{
    /// <summary>Raised whenever a migration changes the schema; never lowered.</summary>
    internal const int Version = 1;

    /// <summary>The engine release that introduced this schema version; bumped together with <see cref="Version"/>.</summary>
    internal const string EngineVersion = "1.0.0";

    /// <summary>
    /// Hold'em's own database schema on PostgreSQL and SQL Server, keeping its tables separate from a
    /// host's own tables and from another game's, which gets a schema of its own.
    /// </summary>
    internal const string DatabaseSchema = "tce_holdem";

    /// <summary>
    /// What stands in for <see cref="DatabaseSchema"/> on SQLite, which has no schemas: every Hold'em
    /// table name starts with it, so a second game's <c>Game</c> table can share the file.
    /// </summary>
    internal const string SqliteTablePrefix = DatabaseSchema + "_";

    /// <summary>
    /// The engine's own migration history table, so that it never collides with a host's EF context
    /// keeping the default <c>__EFMigrationsHistory</c> in the same database. PostgreSQL and SQL Server
    /// keep it in <see cref="DatabaseSchema"/>; SQLite uses <see cref="SqliteMigrationsHistoryTable"/>.
    /// </summary>
    internal const string MigrationsHistoryTable = "__TceMigrationsHistory";

    /// <summary>The history table on SQLite: <see cref="MigrationsHistoryTable"/> with <see cref="SqliteTablePrefix"/>.</summary>
    internal const string SqliteMigrationsHistoryTable = SqliteTablePrefix + MigrationsHistoryTable;

    /// <summary>The table of the single schema version row, before any SQLite prefix.</summary>
    internal const string SchemaTable = "__TceSchema";

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
