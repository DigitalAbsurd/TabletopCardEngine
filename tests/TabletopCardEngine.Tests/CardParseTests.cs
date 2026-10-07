using FluentAssertions;

namespace TabletopCardEngine.Tests;

[TestClass]
public sealed class CardParseTests
{
    private static readonly string[] _invalid = ["", "a", "1h", "ax", "ahh", " ah"];

    private static IEnumerable<Card> AllCards()
        => Enum.GetValues<CardType>().SelectMany(type => Enum.GetValues<CardSuit>().Select(suit => new Card(type, suit)));

    [TestMethod]
    public void CardParse_AllFiftyTwoToStringForms_RoundTrip()
    {
        var cards = AllCards().ToList();
        cards.Should().HaveCount(52);

        foreach (var card in cards)
        {
            Card.Parse(card.ToString()).Should().Be(card);
            Card.TryParse(card.ToString(), out var parsed).Should().BeTrue();
            parsed.Should().Be(card);
        }
    }

    [TestMethod]
    public void Card_Constructor_UndefinedRank_Throws()
    {
        var create = () => new Card((CardType)0, CardSuit.Heart);

        create.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("cardType");
    }

    [TestMethod]
    public void Card_Constructor_UndefinedSuit_Throws()
    {
        var create = () => new Card(CardType.Ace, (CardSuit)9);

        create.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("cardSuit");
    }

    [TestMethod]
    public void Card_Constructor_ValidArguments_ToStringIsCanonical()
        => new Card(CardType.Ace, CardSuit.Heart).ToString().Should().Be("ah");

    [TestMethod]
    public void CardParse_SuitBeforeRank_EqualsRankBeforeSuit()
        => Card.Parse("ha").Should().Be(Card.Parse("ah"));

    [TestMethod]
    public void CardParse_UpperCase_Parses()
        => Card.Parse("AH").Should().Be(Card.Parse("ah"));

    [TestMethod]
    public void CardParse_Null_ThrowsArgumentNullException()
    {
        var parse = () => Card.Parse(null!);

        parse.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void CardParse_NotACard_ThrowsFormatExceptionQuotingInput()
    {
        foreach (var input in _invalid)
        {
            var parse = () => Card.Parse(input);

            parse.Should().Throw<FormatException>().WithMessage($"*'{input}'*", $"'{input}' is not a card");
        }
    }

    [TestMethod]
    public void CardTryParse_NotACard_ReturnsFalseWithNullCard()
    {
        foreach (var input in _invalid.Append(null))
        {
            Card.TryParse(input, out var card).Should().BeFalse($"'{input}' is not a card");
            card.Should().BeNull();
        }
    }

    [TestMethod]
    public void CardParse_ThroughIParsable_IgnoresFormatProvider()
    {
        var parse = static (string s) => ParseGeneric<Card>(s);

        parse("ah").Should().Be(Card.Parse("ah"));
    }

    private static T ParseGeneric<T>(string s) where T : IParsable<T>
        => T.Parse(s, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
}
