#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Options](TabletopCardEngine.Holdem.Options.md 'TabletopCardEngine\.Holdem\.Options')

## HoldemSettings Class

Validation limits applied to every game the engine runs\.

```csharp
public sealed class HoldemSettings
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemSettings
### Properties

<a name='TabletopCardEngine.Holdem.Options.HoldemSettings.EnableSensitiveCardLogging'></a>

## HoldemSettings\.EnableSensitiveCardLogging Property

When `true`, the engine logs every seat's hole cards and the board at Trace level\.
This feature is &lt;b&gt;disabled by default&lt;/b&gt; and must be explicitly enabled — it is never for production\.
The setting is ignored unless the `TabletopCardEngine` category is enabled at Trace level\.

```csharp
public bool EnableSensitiveCardLogging { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.Holdem.Options.HoldemSettings.GamePlayersMaxCount'></a>

## HoldemSettings\.GamePlayersMaxCount Property

Most seats a hand may be dealt to\. Within 2 to 10, and 10 by default; `WithSettings` throws outside
that range\. A game's table format caps its seats as well, so the default 6\-Max table still stops at 6\.

```csharp
public int GamePlayersMaxCount { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.Holdem.Options.HoldemSettings.GamePlayersMinCount'></a>

## HoldemSettings\.GamePlayersMinCount Property

Fewest seats a hand may be dealt to\. Within 2 to 10; `WithSettings` throws outside that range\.

```csharp
public int GamePlayersMinCount { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')
