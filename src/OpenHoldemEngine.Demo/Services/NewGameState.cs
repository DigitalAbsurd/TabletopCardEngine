namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// Lets the header's New Game button open the settings modal, which lives on the game page because
/// applying it also resets that page's own state (the selected hand history).
/// </summary>
public class NewGameState
{
    /// <summary>
    /// Raised when the New Game modal should be opened.
    /// </summary>
    public event Func<Task>? ShowRequested;

    /// <summary>
    /// Requests that the New Game modal be opened.
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
