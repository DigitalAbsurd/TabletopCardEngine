using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OpenHoldemEngine.Demo.Services;
using System.Runtime.CompilerServices;

namespace OpenHoldemEngine.Demo.Components.EngineCalls;

/// <summary>
/// Component that displays the engine call journal with C# syntax highlighting.
/// Shows each call that has been made to the engine, the result, and provides
/// copy-to-clipboard functionality.
/// </summary>
public sealed partial class EngineCalls : ComponentBase, IAsyncDisposable
{
    [Inject]
    public DemoGameSession Session { get; set; } = null!;

    private ElementReference _listElement;
    private IJSObjectReference? _jsModule;
    private int _lastJournalCount;

    // Keyed by the entry itself: a new game restarts the sequence numbers, and an entry of the old
    // game must not hand its open panel or its dump to the new game's call with the same number.
    private readonly HashSet<DumpKey> _openDumps = [];
    private readonly Dictionary<DumpKey, ObjectDumpNode> _dumps = [];

    private static readonly DumpKind[] _resultOnly = [DumpKind.Result];
    private static readonly DumpKind[] _resultAndActions = [DumpKind.Result, DumpKind.AvailableActions];

    /// <summary>Called when the session state changes to update the displayed calls.</summary>
    protected override void OnInitialized()
    {
        Session.StateChanged += OnSessionStateChanged;
    }

    /// <summary>
    /// Called after the component renders. Highlights any new code blocks that were just added
    /// and scrolls the list to show the newest entry only when new entries are added.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/EngineCalls/EngineCalls.razor.js");
            _lastJournalCount = Session.Journal.Entries.Count;
        }

        // Highlight code blocks that haven't been highlighted yet
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("highlightCode");

            // Auto-scroll to the newest entry only if a new entry was actually added
            int currentCount = Session.Journal.Entries.Count;
            if (currentCount > _lastJournalCount)
            {
                await _jsModule.InvokeVoidAsync("scrollToLatestEntry", _listElement);
                _lastJournalCount = currentCount;
            }
        }
    }

    /// <summary>Copies a single code snippet to the clipboard.</summary>
    private async Task CopySnippetAsync(int entryIndex)
    {
        var entry = Session.Journal.Entries[entryIndex];
        var snippet = EngineCallSnippetWriter.GenerateSnippet(entry);
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", snippet);
    }

    /// <summary>Copies the entire session's code to the clipboard.</summary>
    private async Task CopyWholeSessionAsync()
    {
        var snippet = EngineCallSnippetWriter.GenerateWholeSessionSnippet(Session.Journal);
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", snippet);
    }

    private void ToggleDump(DumpKey key, bool active)
    {
        if (active)
        {
            _openDumps.Add(key);
        }
        else
        {
            _openDumps.Remove(key);
            _dumps.Remove(key);
        }
    }

    /// <summary>
    /// The snippet's variables an entry can dump: <c>result</c> always, <c>availableActions</c> only
    /// for a success, the only kind of call whose snippet declares it.
    /// </summary>
    private static DumpKind[] DumpKindsFor(EngineCallJournalEntry entry) =>
        entry.Result is { IsSuccess: true } ? _resultAndActions : _resultOnly;

    private static string DumpName(DumpKind kind) => kind == DumpKind.Result ? "result" : "availableActions";

    /// <summary>The dump of an open panel, built once and kept while the panel stays open.</summary>
    private ObjectDumpNode DumpFor(DumpKey key)
    {
        if (!_dumps.TryGetValue(key, out var dump))
        {
            var result = key.Entry.Result;
            dump = key.Kind == DumpKind.Result
                ? ObjectDump.Create(DumpName(key.Kind), result, typeof(HoldemResult<OpenHoldemEngine.Dto.GameData>))
                : ObjectDump.Create(DumpName(key.Kind), result?.Value.HandData?.Evaluations, typeof(IReadOnlyList<OpenHoldemEngine.Dto.PlayerActionEvaluationData>));
            _dumps[key] = dump;
        }

        return dump;
    }

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        // Forget the panels of entries a new game has cleared out of the journal.
        var live = Session.Journal.Entries.ToHashSet(ReferenceEqualityComparer.Instance);
        _openDumps.RemoveWhere(key => !live.Contains(key.Entry));
        foreach (var stale in _dumps.Keys.Where(key => !_openDumps.Contains(key)).ToList())
        {
            _dumps.Remove(stale);
        }

        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Scrolls the engine calls list to show the latest entry.
    /// Called when the Engine Calls panel is opened/expanded.
    /// </summary>
    public async Task ScrollToLatestEntryAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("scrollToLatestEntry", _listElement);
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        Session.StateChanged -= OnSessionStateChanged;
    }

    private enum DumpKind
    {
        Result,
        AvailableActions,
    }

    /// <summary>
    /// One dump panel: an entry and which of its variables. Compares the entry by reference, for the
    /// reason the fields above give: journal entries are records, and two of them can be equal.
    /// </summary>
    private readonly struct DumpKey(EngineCallJournalEntry entry, DumpKind kind) : IEquatable<DumpKey>
    {
        public EngineCallJournalEntry Entry { get; } = entry;

        public DumpKind Kind { get; } = kind;

        public bool Equals(DumpKey other) => ReferenceEquals(Entry, other.Entry) && Kind == other.Kind;

        public override bool Equals(object? obj) => obj is DumpKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Entry), Kind);
    }
}
