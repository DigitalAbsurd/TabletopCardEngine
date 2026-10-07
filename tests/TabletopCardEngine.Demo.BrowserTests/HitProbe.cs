using System.Globalization;

namespace TabletopCardEngine.Demo.BrowserTests;

/// <summary>
/// What <c>document.elementFromPoint</c> returned at the centre of one element, and the geometry
/// that point was computed from.
/// </summary>
internal sealed record HitProbe
{
    /// <summary>The CSS selector the element was found by.</summary>
    public string Selector { get; init; } = string.Empty;

    /// <summary>The element's trimmed text, which for the action buttons carries the amount.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Whether the element has a box at all and is not hidden by CSS.</summary>
    public bool Rendered { get; init; }

    /// <summary>Whether the element's centre is inside the viewport, i.e. a point a visitor can click.</summary>
    public bool InViewport { get; init; }

    /// <summary>Whether the topmost element at that centre is the element itself or one of its descendants.</summary>
    public bool IsOwnHitTarget { get; init; }

    /// <summary>Tag and classes of whatever was actually on top, for the failure message.</summary>
    public string HitDescription { get; init; } = string.Empty;

    public double Left { get; init; }

    public double Top { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }

    public double CentreX { get; init; }

    public double CentreY { get; init; }

    /// <summary>A one-line description for an assertion message.</summary>
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} ('{1}') at ({2:0.#}, {3:0.#}) size {4:0.#}x{5:0.#}, rendered={6}, inViewport={7}, hit={8}",
            Selector,
            Label,
            CentreX,
            CentreY,
            Width,
            Height,
            Rendered,
            InViewport,
            HitDescription);
    }
}
