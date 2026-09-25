using Microsoft.JSInterop;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// The header's Light / Dark switch, over <c>window.demoTheme</c> in <c>wwwroot/js/theme.js</c>.
/// The script owns the theme — it has to, to set it before Blazor has started — so this service
/// only asks it what is on the page and tells it what the visitor picked.
/// </summary>
public class DemoTheme
{
    private readonly IJSRuntime _js;

    /// <summary>Initializes a new instance of the <see cref="DemoTheme"/> class.</summary>
    /// <param name="js">The JS runtime.</param>
    public DemoTheme(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    /// <summary>
    /// Whether the page is dark now: the visitor's earlier pick, or the OS setting when there is none.
    /// </summary>
    /// <returns><see langword="false"/> as well when the script is not available.</returns>
    public async Task<bool> IsDarkAsync()
    {
        try
        {
            return await _js.InvokeAsync<string>("demoTheme.get") == "dark";
        }
        catch (JSException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            // JS interop is not available (prerendering, or a test renderer).
            return false;
        }
    }

    /// <summary>Applies the visitor's pick and keeps it for the next visit.</summary>
    /// <param name="dark">Dark when <see langword="true"/>, Light otherwise.</param>
    /// <returns>A task that completes once the script has applied the theme.</returns>
    public async Task SetDarkAsync(bool dark)
    {
        try
        {
            await _js.InvokeVoidAsync("demoTheme.set", dark ? "dark" : "light");
        }
        catch (JSException)
        {
            // The script failed to load; the switch then changes nothing, which is all it can do.
        }
        catch (InvalidOperationException)
        {
            // JS interop is not available.
        }
    }
}
