using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OpenHoldemEngine.Demo.Services;

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

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
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
}
