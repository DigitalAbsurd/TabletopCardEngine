using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TabletopCardEngine;

/// <summary>One playing card. Its string form is rank then suit, lowercase — see <see cref="ToString"/>.</summary>
public record Card : IParsable<Card>
{
    private static readonly Dictionary<char, CardSuit> _suitByChar = new()
    {
        { 'c', CardSuit.Club },
        { 'd', CardSuit.Diamond },
        { 'h', CardSuit.Heart },
        { 's', CardSuit.Spade }
    };

    /// <summary>Creates a card; the only way to build one, so every instance has a defined rank and suit.</summary>
    /// <param name="cardType">The card's rank.</param>
    /// <param name="cardSuit">The card's suit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cardType"/> or <paramref name="cardSuit"/> is not a defined member.</exception>
    public Card(CardType cardType, CardSuit cardSuit)
    {
        if (!Enum.IsDefined(cardType))
        {
            throw new ArgumentOutOfRangeException(nameof(cardType), cardType, "The rank is not a defined CardType.");
        }

        if (!Enum.IsDefined(cardSuit))
        {
            throw new ArgumentOutOfRangeException(nameof(cardSuit), cardSuit, "The suit is not a defined CardSuit.");
        }

        CardType = cardType;
        CardSuit = cardSuit;
    }

    /// <summary>The card's rank.</summary>
    public CardType CardType { get; }

    /// <summary>The card's suit.</summary>
    public CardSuit CardSuit { get; }

    /// <summary>Rank then suit, lowercase — e.g. "ah" (ace of hearts), "tc" (ten of clubs), "2s".</summary>
    public override string ToString()
    {
        var typeByte = (byte)CardType;
        var typeChar = typeByte is >= 2 and <= 9 ? typeByte.ToString(CultureInfo.InvariantCulture) : $"{CardType.ToString().ToLowerInvariant()[0]}";
        var suitChar = CardSuit.ToString().ToLowerInvariant()[0];
        return $"{typeChar}{suitChar}";
    }

    /// <summary>
    /// Parses a card from exactly two characters, rank and suit in either order, case-insensitively —
    /// <c>"ah"</c>, <c>"ha"</c> and <c>"AH"</c> all give the ace of hearts.
    /// </summary>
    /// <param name="s">A two-character card token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not a card token.</exception>
    public static Card Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return TryParseCore(s) ?? throw new FormatException($"'{s}' is not a card: expected a rank and a suit, such as \"ah\".");
    }

    /// <summary>Parses a card like <see cref="Parse(string)"/> but reports failure instead of throwing.</summary>
    /// <param name="s">A two-character card token, or <see langword="null"/>.</param>
    /// <param name="card">The parsed card, or <see langword="null"/> when parsing fails.</param>
    /// <returns><see langword="true"/> when <paramref name="s"/> is a card token.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [NotNullWhen(true)] out Card? card)
    {
        card = s is null ? null : TryParseCore(s);
        return card is not null;
    }

    // Explicit, so the format provider (which a card token has no use for) stays out of the way of
    // the one-argument overloads that callers actually use, and CA1305 has nothing to ask for.
    static Card IParsable<Card>.Parse(string s, IFormatProvider? provider) => Parse(s);

    static bool IParsable<Card>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [NotNullWhen(true)] out Card? result)
        => TryParse(s, out result);

    private static Card? TryParseCore(string s)
    {
        if (s.Length != 2)
        {
            return null;
        }

        var first = char.ToLowerInvariant(s[0]);
        var second = char.ToLowerInvariant(s[1]);
        var (rankChar, suitChar) = _suitByChar.ContainsKey(second) ? (first, second) : (second, first);

        if (!_suitByChar.TryGetValue(suitChar, out var suit))
        {
            return null;
        }

        CardType? cardType = rankChar switch
        {
            >= '2' and <= '9' => (CardType)(rankChar - '0'),
            'a' => CardType.Ace,
            't' => CardType.Ten,
            'j' => CardType.Jack,
            'q' => CardType.Queen,
            'k' => CardType.King,
            _ => null
        };

        return cardType is { } type ? new Card(type, suit) : null;
    }
}
