namespace OpenHoldemEngine.Data;

/// <summary>The schema this build of the engine expects, reported as <see cref="HoldemSchemaStatus.EngineSchemaVersion"/>.</summary>
internal static class HoldemSchema
{
    /// <summary>Raised whenever a migration changes the schema; never lowered.</summary>
    internal const int Version = 1;
}
