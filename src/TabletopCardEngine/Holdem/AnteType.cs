namespace TabletopCardEngine.Holdem;

/// <summary>Who posts the ante of a hand.</summary>
public enum AnteType : byte
{
    /// <summary>Every seat dealt in posts the ante.</summary>
    Classic = 1,

    /// <summary>The big blind seat alone posts the ante, after its blind.</summary>
    BigBlind = 2,
}
