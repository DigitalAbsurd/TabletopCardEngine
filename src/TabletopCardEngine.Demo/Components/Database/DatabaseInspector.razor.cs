using AntDesign;
using Microsoft.AspNetCore.Components;
using TabletopCardEngine.Demo.Services;

namespace TabletopCardEngine.Demo.Components.Database;

/// <summary>
/// Base class for the database inspector component.
/// Provides the logic for querying the SQLite database and displaying its schema and rows.
/// </summary>
public abstract class DatabaseInspectorBase : ComponentBase
{
    /// <summary>The SQLite browser service that queries the database directly.</summary>
    [Inject]
    public SqliteBrowser SqliteBrowser { get; set; } = default!;

    /// <summary>The demo game session, used to detect when the database changes.</summary>
    [Inject]
    public DemoGameSession Session { get; set; } = default!;

    /// <summary>The currently selected table name.</summary>
    public string? SelectedTable { get; set; }

    /// <summary>The list of all table names in the database.</summary>
    public IReadOnlyList<string> TableNames { get; set; } = [];

    /// <summary>The columns in the currently selected table.</summary>
    public IReadOnlyList<ColumnInfo> Columns { get; set; } = [];

    /// <summary>The rows being displayed in the grid.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>The total row count of the currently selected table.</summary>
    public long RowCount { get; set; }

    /// <summary>Whether the row count changed since the last action.</summary>
    public bool RowCountChanged { get; set; }

    /// <summary>The current page number in the pagination.</summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>The number of rows to display per page.</summary>
    public const int PageSize = 50;

    /// <summary>The CREATE TABLE statement for the currently selected table.</summary>
    public string? CreateTableStatement { get; set; }

    /// <summary>The last error message encountered during database operations.</summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Initializes the component and loads the table list.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            TableNames = SqliteBrowser.GetTableNames();
            if (TableNames.Count > 0)
            {
                SelectedTable = TableNames[0];
                LoadTableData(SelectedTable);
            }
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
        }

        Session.StateChanged += OnSessionStateChanged;
    }

    /// <summary>
    /// Called when the session state changes. Reloads the table data and highlights changes.
    /// </summary>
    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        if (SelectedTable != null)
        {
            var previousCount = RowCount;
            _ = InvokeAsync(() =>
            {
                try
                {
                    LoadTableData(SelectedTable);
                    RowCountChanged = RowCount != previousCount;
                }
                catch (Exception ex)
                {
                    LastErrorMessage = ex.Message;
                }
                finally
                {
                    StateHasChanged();
                }

                return Task.CompletedTask;
            });
        }
    }

    /// <summary>
    /// Called when the user selects a different table from the dropdown.
    /// </summary>
    public Task OnTableSelectedAsync(string? tableName)
    {
        SelectedTable = tableName;
        CurrentPage = 1;
        RowCountChanged = false;
        try
        {
            LoadTableData(SelectedTable);
            LastErrorMessage = null;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the user navigates to a different page in the pagination.
    /// </summary>
    public Task OnPageChangedAsync(PaginationEventArgs args)
    {
        // Get the current page from the event args
        CurrentPage = (int)args.Page;
        try
        {
            LoadTableData(SelectedTable);
            LastErrorMessage = null;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the schema and rows for the given table.
    /// Runs inline on the caller's context instead of on a thread-pool thread, because the connection is shared with the engine and is not thread-safe.
    /// </summary>
    private void LoadTableData(string? tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        try
        {
            Columns = SqliteBrowser.GetColumnInfo(tableName);
            RowCount = SqliteBrowser.GetRowCount(tableName);
            CreateTableStatement = SqliteBrowser.GetCreateTableStatement(tableName);

            var offset = (CurrentPage - 1) * PageSize;
            Rows = SqliteBrowser.GetTableRows(tableName, PageSize, offset);
            LastErrorMessage = null;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            Columns = [];
            Rows = [];
            RowCount = 0;
            CreateTableStatement = null;
            throw;
        }
    }

    /// <summary>
    /// Formats a column header, adding a 🔑 indicator for primary key columns.
    /// </summary>
    protected static string FormatColumnHeader(ColumnInfo column)
    {
        var header = column.Name;
        if (column.IsPrimaryKey)
        {
            header += " 🔑";
        }

        return header;
    }

    /// <summary>
    /// Formats a cell value for display, showing "NULL" for null values.
    /// </summary>
    protected static string FormatCellValue(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value))
        {
            return "NULL";
        }

        if (value is null)
        {
            return "NULL";
        }

        return value.ToString() ?? "(empty)";
    }
}
