using Microsoft.Data.Sqlite;
using System.Data;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// Queries the SQLite database directly using a raw connection, bypassing EF.
/// This is deliberate: the point of the database inspector is to show the physical schema and data,
/// not the mapped entities. All queries work against what the engine actually wrote to the database.
/// </summary>
public sealed class SqliteBrowser
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Creates a new SQLite browser that queries the same connection the engine uses.
    /// </summary>
    /// <param name="connection">The in-memory SQLite connection the engine writes through.</param>
    public SqliteBrowser(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Gets the list of all non-system tables in the database.
    /// </summary>
    /// <returns>Ordered list of table names, excluding sqlite_% system tables.</returns>
    public IReadOnlyList<string> GetTableNames()
    {
        var tables = new List<string>();

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT ""name""
            FROM sqlite_master
            WHERE ""type"" = 'table'
              AND ""name"" NOT LIKE 'sqlite_%'
            ORDER BY ""name""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <summary>
    /// Gets the CREATE TABLE statement for the specified table.
    /// </summary>
    /// <param name="tableName">The table name (must come from GetTableNames).</param>
    /// <returns>The SQL CREATE TABLE statement, or null if the table does not exist.</returns>
    public string? GetCreateTableStatement(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return null;
        }

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT ""sql""
            FROM sqlite_master
            WHERE ""type"" = 'table'
              AND ""name"" = @tableName";

        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = command.ExecuteReader();
        return reader.Read() ? reader.GetString(0) : null;
    }

    /// <summary>
    /// Gets column information for a table from PRAGMA table_info.
    /// </summary>
    /// <param name="tableName">The table name (must come from GetTableNames).</param>
    /// <returns>List of column definitions, in order.</returns>
    public IReadOnlyList<ColumnInfo> GetColumnInfo(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return [];
        }

        var columns = new List<ColumnInfo>();

        using var command = _connection.CreateCommand();
        // Use PRAGMA table_info which is SQLite standard.
        // Columns are: cid, name, type, notnull, dflt_value, pk
        command.CommandText = $"PRAGMA table_info({EscapeIdentifier(tableName)})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var isPrimaryKey = !reader.IsDBNull(5) && reader.GetInt32(5) > 0;
            columns.Add(new ColumnInfo(
                Name: reader.GetString(1),
                Type: reader.GetString(2),
                NotNull: reader.GetBoolean(3),
                IsPrimaryKey: isPrimaryKey,
                PrimaryKeyOrder: isPrimaryKey ? reader.GetInt32(5) : 0
            ));
        }

        return columns;
    }

    /// <summary>
    /// Gets the total row count for a table.
    /// </summary>
    /// <param name="tableName">The table name (must come from GetTableNames).</param>
    /// <returns>The number of rows in the table, or 0 if the table does not exist.</returns>
    public long GetRowCount(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return 0;
        }

        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {EscapeIdentifier(tableName)}";

        var result = command.ExecuteScalar();
        return result is long count ? count : 0;
    }

    /// <summary>
    /// Gets a page of rows from a table with optional limit and offset.
    /// </summary>
    /// <param name="tableName">The table name (must come from GetTableNames).</param>
    /// <param name="limit">Maximum number of rows to return (typically 50).</param>
    /// <param name="offset">Number of rows to skip (for paging).</param>
    /// <returns>List of rows, each as a dictionary of column name -> value.</returns>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetTableRows(
        string tableName,
        int limit = 50,
        int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(tableName) || limit <= 0)
        {
            return [];
        }

        var rows = new List<IReadOnlyDictionary<string, object?>>();

        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {EscapeIdentifier(tableName)} LIMIT @limit OFFSET @offset";
        command.Parameters.AddWithValue("@limit", limit);
        command.Parameters.AddWithValue("@offset", Math.Max(0, offset));

        using var reader = command.ExecuteReader();
        var columnCount = reader.FieldCount;
        var columnNames = Enumerable.Range(0, columnCount)
            .Select(i => reader.GetName(i))
            .ToList();

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < columnCount; i++)
            {
                row[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Escapes a SQLite identifier (table or column name) by doubling quotes.
    /// </summary>
    /// <param name="identifier">The unescaped identifier.</param>
    /// <returns>The escaped identifier, safe for SQL.</returns>
    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "\"\"";
        }

        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}

/// <summary>
/// Information about a table column from PRAGMA table_info.
/// </summary>
public sealed record ColumnInfo(
    string Name,
    string Type,
    bool NotNull,
    bool IsPrimaryKey,
    int PrimaryKeyOrder);
