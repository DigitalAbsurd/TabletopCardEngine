namespace TabletopCardEngine;

/// <summary>
/// A card's rank. The numeric value is the pip count for 2-10; the ace is <c>1</c> here but ranks
/// above the king when hands are compared.
/// </summary>
public enum CardType : byte
{
    /// <summary>Ace.</summary>
    Ace = 1,

    /// <summary>Two.</summary>
    Two = 2,

    /// <summary>Three.</summary>
    Three = 3,

    /// <summary>Four.</summary>
    Four = 4,

    /// <summary>Five.</summary>
    Five = 5,

    /// <summary>Six.</summary>
    Six = 6,

    /// <summary>Seven.</summary>
    Seven = 7,

    /// <summary>Eight.</summary>
    Eight = 8,

    /// <summary>Nine.</summary>
    Nine = 9,

    /// <summary>Ten.</summary>
    Ten = 10,

    /// <summary>Jack.</summary>
    Jack = 11,

    /// <summary>Queen.</summary>
    Queen = 12,

    /// <summary>King.</summary>
    King = 13
}
