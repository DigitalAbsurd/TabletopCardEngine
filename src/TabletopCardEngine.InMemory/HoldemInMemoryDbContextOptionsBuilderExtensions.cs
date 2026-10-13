using Microsoft.EntityFrameworkCore;

namespace TabletopCardEngine.InMemory;

/// <summary>
/// Configures the engine's database as EF Core InMemory.
/// </summary>
public static class HoldemInMemoryDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Uses an in-memory database for the engine, with the given name as the store identifier. Two
    /// engines given the same name will share the same in-memory store; use a unique name for each
    /// isolated engine (one per test, one per tenant, etc.).
    /// </summary>
    /// <param name="builder">The options passed to <c>AddHoldem</c>.</param>
    /// <param name="databaseName">A unique name for this in-memory store.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// The InMemory provider is for prototyping, testing and isolated unit testing, not for
    /// production. It has four limits: state lives only in the process and is discarded on restart;
    /// foreign keys are not enforced; transactions are ignored; and <see cref="decimal"/> precision
    /// is not applied (although the engine's own two-decimal rule is). See the persistence
    /// documentation for details.
    /// </remarks>
    public static DbContextOptionsBuilder UseHoldemInMemory(this DbContextOptionsBuilder builder, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        builder.UseInMemoryDatabase(databaseName);
        return builder;
    }
}
