using Microsoft.JSInterop;

namespace TabletopCardEngine.Demo.Services;

/// <summary>
/// Provides typed access to the browser's localStorage, with proper error handling.
/// </summary>
/// <remarks>
/// This service wraps localStorage access through an ES module to avoid the type mismatch
/// that occurs when trying to deserialise localStorage.getItem's string-or-null return
/// into a C# bool. JavaScript handles the string conversion; the C# side sees clean booleans.
/// </remarks>
public class BrowserStorage : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserStorage"/> class.
    /// </summary>
    public BrowserStorage(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    /// <summary>
    /// Reads a boolean flag from localStorage, returning false if not set or if localStorage is unavailable.
    /// </summary>
    /// <param name="key">The localStorage key.</param>
    /// <returns>
    /// <c>true</c> if the key's stored value is "true" (case-insensitive), <c>false</c> otherwise
    /// or if localStorage is unavailable.
    /// </returns>
    public async Task<bool> GetFlagAsync(string key)
    {
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/browserStorage.js");
            return await _module.InvokeAsync<bool>("getFlag", key);
        }
        catch (JSException)
        {
            // localStorage or module loading failed; default to false.
            return false;
        }
        catch (InvalidOperationException)
        {
            // JS interop is not available (e.g., during prerender); default to false.
            return false;
        }
    }

    /// <summary>
    /// Writes a boolean flag to localStorage, silently failing if localStorage is unavailable.
    /// </summary>
    /// <param name="key">The localStorage key.</param>
    /// <param name="value">The value to store ("true" or "false").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetFlagAsync(string key, bool value)
    {
        try
        {
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/browserStorage.js");
            await _module.InvokeVoidAsync("setFlag", key, value);
        }
        catch (JSException)
        {
            // localStorage or module loading failed; silently ignore.
        }
        catch (InvalidOperationException)
        {
            // JS interop is not available; silently ignore.
        }
    }

    /// <summary>
    /// Disposes the cached JS module reference.
    /// </summary>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch
            {
                // Ignore errors during module disposal.
            }
        }

        GC.SuppressFinalize(this);
    }
}
