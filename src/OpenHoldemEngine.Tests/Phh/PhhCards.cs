namespace OpenHoldemEngine.Tests.Phh;

/// <summary>
/// Card notation of the PHH format: rank char then suit char, uppercase rank and lowercase suit
/// (<c>Ac</c>, <c>Th</c>, <c>2d</c>), with <c>??</c> standing for an unknown card. Cards are
/// concatenated without a separator, so <c>3c9s</c> is two cards.
/// </summary>
internal static class PhhCards
{
    private const int CardTokenLength = 2;
    private const string UnknownCard = "??";

    /// <summary>The 52-card deck in a fixed order, used to fill in cards a hand history omits.</summary>
    internal static readonly IReadOnlyList<Card> FullDeck = [.. Enum.GetValues<CardType>()
        .SelectMany(cardType => Enum.GetValues<CardSuit>()
            .Select(cardSuit => new Card { CardType = cardType, CardSuit = cardSuit }))];

    /// <summary>Parses a run of concatenated cards; <c>null</c> marks an unknown (<c>??</c>) card.</summary>
    internal static Card?[] ParseMany(string cards)
    {
        if (cards.Length % CardTokenLength != 0)
        {
            throw new FormatException($"'{cards}' is not a whole number of PHH cards");
        }

        return [.. Enumerable.Range(0, cards.Length / CardTokenLength).Select(ix => Parse(cards.Substring(ix * CardTokenLength, CardTokenLength)))];
    }

    internal static Card? Parse(string card)
    {
        if (card == UnknownCard)
        {
            return null;
        }

        // Card.Parse takes the lowercase rank-then-suit form the engine itself writes; PHH
        // differs only in the uppercase rank, and rank chars never collide with suit chars.
        return Card.Parse(card.ToLowerInvariant());
    }
}
