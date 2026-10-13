namespace TabletopCardEngine.Holdem.Data;

/// <summary>
/// Records the schema version and engine version that created this database. The engine writes one
/// row on creation via model seed data, and migrations update it when <see cref="HoldemSchema.Version"/>
/// is bumped. A newer database is <see cref="HoldemSchemaState.NewerThanEngine"/>; an older or
/// missing one is <see cref="HoldemSchemaState.MigrationRequired"/>.
/// </summary>
internal sealed class TceSchemaRow
{
    /// <summary>Single-row table, always Id = 1.</summary>
    internal int Id { get; set; }

    /// <summary>The schema version that created this database.</summary>
    internal int SchemaVersion { get; set; }

    /// <summary>The engine version that created this database.</summary>
    internal string EngineVersion { get; set; } = string.Empty;
}
