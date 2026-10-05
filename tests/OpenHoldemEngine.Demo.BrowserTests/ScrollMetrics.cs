using System.Globalization;

namespace OpenHoldemEngine.Demo.BrowserTests;

/// <summary>
/// The scroll offsets and sizes of the page's scroll container, read straight off the element.
/// </summary>
internal sealed record ScrollMetrics
{
    /// <summary>The selector the element was read from, empty for the page scroller.</summary>
    public string Selector { get; init; } = string.Empty;

    /// <summary>Whether the element was found at all.</summary>
    public bool Found { get; init; }

    /// <summary>
    /// The element's computed <c>overflow</c>. <c>auto</c> is what makes an element a scroll
    /// container; <c>visible</c> is what the demo's layout reported while its scoped stylesheet
    /// never applied.
    /// </summary>
    public string Overflow { get; init; } = string.Empty;

    public double ScrollLeft { get; init; }

    public double ScrollTop { get; init; }

    public double ScrollWidth { get; init; }

    public double ClientWidth { get; init; }

    public double ScrollHeight { get; init; }

    public double ClientHeight { get; init; }

    /// <summary>A one-line description for an assertion message.</summary>
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{6}: overflow={7}, scrollLeft={0:0.#}, scrollTop={1:0.#}, scrollWidth={2:0.#}, clientWidth={3:0.#}, scrollHeight={4:0.#}, clientHeight={5:0.#}",
            ScrollLeft,
            ScrollTop,
            ScrollWidth,
            ClientWidth,
            ScrollHeight,
            ClientHeight,
            Selector.Length == 0 ? "document.scrollingElement" : Selector,
            Overflow);
    }
}
