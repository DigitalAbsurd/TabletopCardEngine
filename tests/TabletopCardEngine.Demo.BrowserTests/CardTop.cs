using System.Globalization;

namespace TabletopCardEngine.Demo.BrowserTests;

/// <summary>The top edge of one table card, read with <c>getBoundingClientRect</c>, and whether it is lit.</summary>
internal sealed record CardTop
{
    public bool Highlighted { get; init; }

    public double Top { get; init; }

    /// <summary>A one-line description for an assertion message.</summary>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "top={0:0.#}, highlighted={1}", Top, Highlighted);
    }
}
