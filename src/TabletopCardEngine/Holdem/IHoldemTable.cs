using TabletopCardEngine.Holdem.Dto;

namespace TabletopCardEngine.Holdem;

/// <summary>
/// A Hold'em table: the single entry point for game commands, and every game action goes through <see cref="ApplyAsync"/>.
/// There is no long-lived in-memory game: every call rehydrates the aggregate from the database, applies one action,
/// and projects the result back out.
/// <para>
/// <b>Scope of this release:</b> No-Limit Texas Hold'em cash games at 2 to 6 seats, with no antes and no rake.
/// Other betting structures, more seats, antes and tournaments are planned as later minor releases (see <c>docs/roadmap.md</c>),
/// and <see cref="GameOptionData"/> is where they will plug in.
/// </para>
/// <para>
/// The table is obtained from the dependency-injection container registered by
/// <c>AddHoldemEngine</c>: <c>services.GetRequiredService&lt;IHoldemTable&gt;()</c>. It is an interface so that a
/// consumer's tests can substitute it.
/// </para>
/// </summary>
public interface IHoldemTable
{
    /// <summary>
    /// Applies one action to a game and returns the game as it now stands.
    /// <para>
    /// A failed result short-circuits before the transaction is saved, so a validation failure
    /// leaves nothing persisted.
    /// </para>
    /// <para>
    /// <b>Concurrency:</b> <see cref="GameData.Version"/> counts the saves of a game, starting at <c>1</c>
    /// on creation; every call that saves increments it, and reading the game does not. The call fails with
    /// <see cref="HoldemErrorCode.Conflict"/> when <see cref="HoldemAction.ExpectedVersion"/> is set and no
    /// longer matches, or when another writer saved the game between this call's load and its save. Nothing
    /// is persisted on a conflict, and the engine never retries: the caller reloads with
    /// <see cref="HoldemAction.GetGame(Guid)"/> and decides.
    /// </para>
    /// <para>
    /// <b>Schema check:</b> the first call on each table instance checks the database schema version before it
    /// does anything else, and a failed check is not cached, so the next call checks again.
    /// </para>
    /// </summary>
    /// <param name="action">
    /// The action to apply. Its concrete record type decides which handler runs.
    /// </param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>
    /// The projected game on success, or a failed result carrying the validation errors, each with a stable <see cref="HoldemErrorCode"/>.
    /// </returns>
    /// <exception cref="HoldemSchemaException">
    /// On the first call, when the database is newer than the engine, needs a migration, or is not versioned.
    /// </exception>
    /// <exception cref="OperationCanceledException">When <paramref name="cancellationToken"/> is cancelled.</exception>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">
    /// When the save fails for any reason other than a concurrency conflict; that is a fault, not a result.
    /// </exception>
    Task<HoldemResult<GameData>> ApplyAsync(HoldemAction action, CancellationToken cancellationToken = default);
}
