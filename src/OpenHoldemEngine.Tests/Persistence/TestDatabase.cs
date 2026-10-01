using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenHoldemEngine.InMemory;
using OpenHoldemEngine.Sqlite;

namespace OpenHoldemEngine.Tests.Persistence;

/// <summary>The databases the provider matrix runs on, one row per supported provider package.</summary>
public enum TestDatabaseKind
{
    Sqlite,
    InMemory,
    PostgreSql,
    SqlServer,
}

/// <summary>
/// Gives a test its own isolated database of a given kind, so the matrix tests keep the assembly's
/// method-level parallelism.
/// </summary>
internal static class TestDatabase
{
    /// <summary>Whether the kind is a relational database with migrations of its own.</summary>
    internal static bool IsRelational(TestDatabaseKind kind) => kind != TestDatabaseKind.InMemory;

    /// <summary>
    /// Returns the registration call for a fresh database of <paramref name="kind"/>. The action can be
    /// applied to several containers to put them over the same database; <paramref name="lifetime"/>
    /// releases what the database holds (the kept-alive connection) and must outlive all of them.
    /// </summary>
    internal static Action<DbContextOptionsBuilder> Configure(TestDatabaseKind kind, out IDisposable lifetime)
    {
        switch (kind)
        {
            case TestDatabaseKind.Sqlite:
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                lifetime = connection;
                return options => options.UseHoldemSqlite(connection);
            case TestDatabaseKind.InMemory:
                var databaseName = Guid.NewGuid().ToString("N");
                lifetime = NoopLifetime.Instance;
                return options => options.UseHoldemInMemory(databaseName);
            default:
                Assert.Inconclusive($"{kind} joins the provider matrix with the Testcontainers issue.");
                throw new InvalidOperationException("Unreachable: Assert.Inconclusive throws.");
        }
    }

    private sealed class NoopLifetime : IDisposable
    {
        internal static readonly NoopLifetime Instance = new();

        public void Dispose()
        {
        }
    }
}
