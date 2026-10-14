#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine](TabletopCardEngine.md 'TabletopCardEngine')

## Card Class

One playing card\. Its string form is rank then suit, lowercase — see [ToString\(\)](TabletopCardEngine.Card.md#TabletopCardEngine.Card.ToString() 'TabletopCardEngine\.Card\.ToString\(\)')\.

```csharp
public record Card : System.IParsable<TabletopCardEngine.Card>, System.IEquatable<TabletopCardEngine.Card>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → Card

Implements [System\.IParsable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iparsable-1 'System\.IParsable\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iparsable-1 'System\.IParsable\`1'), [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Constructors

<a name='TabletopCardEngine.Card.Card(TabletopCardEngine.CardType,TabletopCardEngine.CardSuit)'></a>

## Card\(CardType, CardSuit\) Constructor

Creates a card; the only way to build one, so every instance has a defined rank and suit\.

```csharp
public Card(TabletopCardEngine.CardType cardType, TabletopCardEngine.CardSuit cardSuit);
```
#### Parameters

<a name='TabletopCardEngine.Card.Card(TabletopCardEngine.CardType,TabletopCardEngine.CardSuit).cardType'></a>

`cardType` [CardType](TabletopCardEngine.CardType.md 'TabletopCardEngine\.CardType')

The card's rank\.

<a name='TabletopCardEngine.Card.Card(TabletopCardEngine.CardType,TabletopCardEngine.CardSuit).cardSuit'></a>

`cardSuit` [CardSuit](TabletopCardEngine.CardSuit.md 'TabletopCardEngine\.CardSuit')

The card's suit\.

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
[cardType](TabletopCardEngine.Card.md#TabletopCardEngine.Card.Card(TabletopCardEngine.CardType,TabletopCardEngine.CardSuit).cardType 'TabletopCardEngine\.Card\.Card\(TabletopCardEngine\.CardType, TabletopCardEngine\.CardSuit\)\.cardType') or [cardSuit](TabletopCardEngine.Card.md#TabletopCardEngine.Card.Card(TabletopCardEngine.CardType,TabletopCardEngine.CardSuit).cardSuit 'TabletopCardEngine\.Card\.Card\(TabletopCardEngine\.CardType, TabletopCardEngine\.CardSuit\)\.cardSuit') is not a defined member\.
### Properties

<a name='TabletopCardEngine.Card.CardSuit'></a>

## Card\.CardSuit Property

The card's suit\.

```csharp
public TabletopCardEngine.CardSuit CardSuit { get; }
```

#### Property Value
[CardSuit](TabletopCardEngine.CardSuit.md 'TabletopCardEngine\.CardSuit')

<a name='TabletopCardEngine.Card.CardType'></a>

## Card\.CardType Property

The card's rank\.

```csharp
public TabletopCardEngine.CardType CardType { get; }
```

#### Property Value
[CardType](TabletopCardEngine.CardType.md 'TabletopCardEngine\.CardType')
### Methods

<a name='TabletopCardEngine.Card.Parse(string)'></a>

## Card\.Parse\(string\) Method

Parses a card from exactly two characters, rank and suit in either order, case\-insensitively —
`"ah"`, `"ha"` and `"AH"` all give the ace of hearts\.

```csharp
public static TabletopCardEngine.Card Parse(string s);
```
#### Parameters

<a name='TabletopCardEngine.Card.Parse(string).s'></a>

`s` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A two\-character card token\.

#### Returns
[Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
[s](TabletopCardEngine.Card.md#TabletopCardEngine.Card.Parse(string).s 'TabletopCardEngine\.Card\.Parse\(string\)\.s') is [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

[System\.FormatException](https://learn.microsoft.com/en-us/dotnet/api/system.formatexception 'System\.FormatException')  
[s](TabletopCardEngine.Card.md#TabletopCardEngine.Card.Parse(string).s 'TabletopCardEngine\.Card\.Parse\(string\)\.s') is not a card token\.

<a name='TabletopCardEngine.Card.ToString()'></a>

## Card\.ToString\(\) Method

Rank then suit, lowercase — e\.g\. "ah" \(ace of hearts\), "tc" \(ten of clubs\), "2s"\.

```csharp
public override string ToString();
```

#### Returns
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.Card.TryParse(string,TabletopCardEngine.Card)'></a>

## Card\.TryParse\(string, Card\) Method

Parses a card like [Parse\(string\)](TabletopCardEngine.Card.md#TabletopCardEngine.Card.Parse(string) 'TabletopCardEngine\.Card\.Parse\(string\)') but reports failure instead of throwing\.

```csharp
public static bool TryParse(string? s, out TabletopCardEngine.Card? card);
```
#### Parameters

<a name='TabletopCardEngine.Card.TryParse(string,TabletopCardEngine.Card).s'></a>

`s` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A two\-character card token, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='TabletopCardEngine.Card.TryParse(string,TabletopCardEngine.Card).card'></a>

`card` [Card](TabletopCardEngine.Card.md 'TabletopCardEngine\.Card')

The parsed card, or [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null') when parsing fails\.

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')  
[true](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool') when [s](TabletopCardEngine.Card.md#TabletopCardEngine.Card.TryParse(string,TabletopCardEngine.Card).s 'TabletopCardEngine\.Card\.TryParse\(string, TabletopCardEngine\.Card\)\.s') is a card token\.
