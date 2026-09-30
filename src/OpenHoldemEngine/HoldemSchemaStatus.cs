namespace OpenHoldemEngine;

/// <summary>
/// What <see cref="IHoldemSchemaManager.GetSchemaStatusAsync"/> found. Two statuses read from the
/// same database compare equal, <see cref="PendingMigrations"/> included, element by element.
/// </summary>
/// <param name="State">How the database's schema compares with the engine's.</param>
/// <param name="ProviderName">The EF Core provider name, such as <c>Microsoft.EntityFrameworkCore.Sqlite</c>.</param>
/// <param name="EngineSchemaVersion">The schema version this engine expects.</param>
/// <param name="DatabaseSchemaVersion">The schema version recorded in the database, or <c>null</c> when none is recorded.</param>
/// <param name="DatabaseEngineVersion">The version of the engine that last migrated the database, or <c>null</c> when none is recorded.</param>
/// <param name="PendingMigrations">The migrations the database has not applied yet, in the order they would be applied; empty when there are none.</param>
public sealed record HoldemSchemaStatus(
    HoldemSchemaState State,
    string ProviderName,
    int EngineSchemaVersion,
    int? DatabaseSchemaVersion,
    string? DatabaseEngineVersion,
    IReadOnlyList<string> PendingMigrations);
