using Microsoft.Data.Sqlite;
using TabletopCardEngine.Holdem.Data;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Empties the demo's only real data source: the rows the engine has written to the in-memory
/// SQLite database.
/// </summary>
/// <remarks>
/// The rows go, the schema stays. Deleting the database through EF closes the connection, and here
/// the connection <em>is</em> the database — the kept-alive <see cref="SqliteConnection"/> opened by
/// <c>DemoEngineHost</c> is what holds <c>:memory:</c> in existence, and <see cref="SqliteBrowser"/>
/// reads the very same object. Dropping the schema out from under both to recreate it identically
/// buys nothing, because the schema never changes between games.
/// </remarks>
public sealed class DemoDatabase
{
    private readonly SqliteConnection _connection;
    private readonly SqliteBrowser _browser;

    /// <summary>
    /// Creates a database reset over the connection the engine writes through.
    /// </summary>
    /// <param name="connection">The demo's kept-alive in-memory SQLite connection.</param>
    /// <param name="browser">The reader that already knows which tables the engine created.</param>
    public DemoDatabase(SqliteConnection connection, SqliteBrowser browser)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
    }

    /// <summary>
    /// Deletes every row of every engine table, leaving the schema in place.
    /// <para>
    /// Tables whose names start with <c>__</c>, after the <c>tce_holdem_</c> prefix of the engine's own
    /// tables, are bookkeeping (the schema version, the migration history, EF's migration lock) and are
    /// never cleared.
    /// </para>
    /// </summary>
    public void ClearAllData()
    {
        var tableNames = _browser.GetTableNames()
            .Where(name => !IsBookkeeping(name))
            .ToList();
        if (tableNames.Count == 0)
        {
            return;
        }

        using var transaction = _connection.BeginTransaction();
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;

        // Microsoft.Data.Sqlite turns foreign keys on by default, so an arbitrary delete order
        // would fail on the first parent row. Deferring the checks to the commit makes the order
        // irrelevant: by then every table is empty and nothing can be left dangling.
        command.CommandText = "PRAGMA defer_foreign_keys = ON;"
            + string.Concat(tableNames.Select(tableName => $"DELETE FROM {QuoteIdentifier(tableName)};"));
        command.ExecuteNonQuery();

        transaction.Commit();
    }

    private static bool IsBookkeeping(string tableName)
    {
        var unprefixed = tableName.StartsWith(HoldemSchema.SqliteTablePrefix, StringComparison.Ordinal)
            ? tableName[HoldemSchema.SqliteTablePrefix.Length..]
            : tableName;
        return unprefixed.StartsWith("__", StringComparison.Ordinal);
    }

    /// <summary>Quotes a table name the same way <see cref="SqliteBrowser"/> does, by doubling quotes.</summary>
    private static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
