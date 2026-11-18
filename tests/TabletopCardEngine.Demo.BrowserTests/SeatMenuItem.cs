namespace TabletopCardEngine.Demo.BrowserTests;

/// <summary>One item of an open seat menu, as the page reports it.</summary>
internal sealed record SeatMenuItem
{
    public string Label { get; init; } = string.Empty;

    public bool Disabled { get; init; }
}
