namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// Shared state service for the walkthrough component, allowing the header button to request showing it.
/// </summary>
public class WalkthroughState
{
    /// <summary>
    /// Raised when the walkthrough should be shown, typically from the header button.
    /// </summary>
    public event Func<Task>? ShowRequested;

    /// <summary>
    /// Requests that the walkthrough be shown.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RequestShowAsync()
    {
        if (ShowRequested is not null)
        {
            await ShowRequested.Invoke();
        }
    }
}
