using Microsoft.EntityFrameworkCore;
using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Holdem;

/// <summary>
/// The save of an aggregate whose <c>Version</c> is its optimistic concurrency token: a <see cref="Game"/> or a
/// <see cref="Tournament"/>. Both are keyed on a <see cref="Guid"/> <c>Id</c> and carry an <see cref="int"/>
/// <c>Version</c> the caller has already incremented, so one implementation answers a lost race the same way for both.
/// </summary>
internal static class VersionedSave
{
    private const string IdProperty = "Id";
    private const string VersionProperty = "Version";

    /// <summary>Saves the change set, or reports that another writer saved the aggregate first.</summary>
    /// <param name="dbContext">The context tracking the aggregate and its changes.</param>
    /// <param name="aggregate">The aggregate root, with <c>Version</c> already incremented when it existed.</param>
    /// <param name="existed">Whether the aggregate was loaded rather than created by this call: a new one has no stored version to race on.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns><see langword="false"/> when another writer saved first; nothing is persisted then.</returns>
    internal static async Task<bool> TrySaveAsync<TAggregate>(HoldemDbContext dbContext, TAggregate aggregate, bool existed, CancellationToken cancellationToken)
        where TAggregate : class
    {
        if (existed && await LostRaceBeforeSaveAsync(dbContext, aggregate, cancellationToken))
        {
            return false;
        }

        try
        {
            if (existed && dbContext.Database.IsRelational())
            {
                return await SaveVersionFirstAsync(dbContext, aggregate, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    /// <summary>
    /// The <c>Version</c> check, run ahead of the save on a non-relational provider. A relational database
    /// checks the token in the <c>UPDATE</c> statement, inside the transaction that also inserts the new
    /// rows. EF's InMemory provider has no transaction and applies every insert before any update, so a lost
    /// race there fails on a duplicate child key, with an exception that names no conflict,
    /// before the token is ever compared. This narrows the window rather than closing it, which is all a
    /// store without transactions allows. It only stands in for the token, so it is skipped when
    /// <c>Version</c> is not one.
    /// </summary>
    private static async Task<bool> LostRaceBeforeSaveAsync<TAggregate>(HoldemDbContext dbContext, TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : class
    {
        if (dbContext.Database.IsRelational())
        {
            return false;
        }

        var entry = dbContext.Entry(aggregate);
        var version = entry.Property<int>(VersionProperty);
        if (!version.Metadata.IsConcurrencyToken)
        {
            return false;
        }

        var id = entry.Property<Guid>(IdProperty).CurrentValue;
        var storedVersion = await dbContext.Set<TAggregate>()
            .AsNoTracking()
            .Where(candidate => EF.Property<Guid>(candidate, IdProperty) == id)
            .Select(candidate => EF.Property<int>(candidate, VersionProperty))
            .SingleAsync(cancellationToken);
        return storedVersion != version.OriginalValue;
    }

    /// <summary>
    /// The save on a relational database, with the <c>Version</c> check run as a statement of its own before
    /// any child row is written. A provider that batches the save (Npgsql, SqlClient) sends the token check and
    /// the new child inserts together, so a lost race used to fail on a duplicate key, with an
    /// exception that names no conflict, before EF looked at the <c>UPDATE</c> row count. Here the
    /// conditional update runs first inside one transaction: a writer that lost the race updates no row and
    /// rolls back with nothing written, and one that is still racing waits on the row's write lock, which the
    /// update takes anyway, and then sees the new version. The rest of the change set is saved after it, in the
    /// same transaction, against the version this call just wrote.
    /// </summary>
    /// <returns><see langword="false"/> when another writer saved the aggregate first; nothing is persisted then.</returns>
    private static Task<bool> SaveVersionFirstAsync<TAggregate>(HoldemDbContext dbContext, TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : class
    {
        var entry = dbContext.Entry(aggregate);
        var version = entry.Property<int>(VersionProperty);
        var id = entry.Property<Guid>(IdProperty).CurrentValue;
        var loadedVersion = version.OriginalValue;
        var newVersion = version.CurrentValue;

        // A user-initiated transaction has to run inside the execution strategy, or a consumer's
        // EnableRetryOnFailure rejects it. A retry repeats the whole unit, which is why both versions are read
        // before the first attempt rather than from the entry the attempt changes.
        return dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            var updated = await dbContext.Set<TAggregate>()
                .Where(candidate => EF.Property<Guid>(candidate, IdProperty) == id
                    && EF.Property<int>(candidate, VersionProperty) == loadedVersion)
                .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => EF.Property<int>(candidate, VersionProperty), newVersion), ct);
            if (updated == 0)
            {
                return false;
            }

            // The row already holds the new version, so the change set's own token check is made against it.
            version.OriginalValue = newVersion;
            await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct);
            await transaction.CommitAsync(ct);
            dbContext.ChangeTracker.AcceptAllChanges();
            return true;
        }, cancellationToken);
    }
}
