using System.Globalization;

namespace OpenHoldemEngine.Demo.BrowserTests;

/// <summary>
/// The horizontal extent of one element's box, read with <c>getBoundingClientRect</c>.
/// </summary>
internal sealed record ElementBox
{
    /// <summary>The selector the element was read from.</summary>
    public string Selector { get; init; } = string.Empty;

    public double Left { get; init; }

    public double Right { get; init; }

    public double Width { get; init; }

    /// <summary>A one-line description for an assertion message.</summary>
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}: left={1:0.#}, right={2:0.#}, width={3:0.#}",
            Selector,
            Left,
            Right,
            Width);
    }
}
