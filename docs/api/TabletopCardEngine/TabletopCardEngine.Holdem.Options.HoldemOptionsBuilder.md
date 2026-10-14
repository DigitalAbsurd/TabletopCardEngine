#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem\.Options](TabletopCardEngine.Holdem.Options.md 'TabletopCardEngine\.Holdem\.Options')

## HoldemOptionsBuilder Class

Configures the engine from `HoldemBuilderExtensions.AddHoldem`\.

Each builder writes to the engine options of the one container it was created for, so
configuring one engine leaves every other engine in the process untouched.

```csharp
public sealed class HoldemOptionsBuilder
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemOptionsBuilder
### Methods

<a name='TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.WithCardShuffler(TabletopCardEngine.Holdem.ICardShuffler)'></a>

## HoldemOptionsBuilder\.WithCardShuffler\(ICardShuffler\) Method

Replaces the random shuffle with pre\-determined cards, which is what makes replaying a
recorded hand history possible\.

```csharp
public TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder WithCardShuffler(TabletopCardEngine.Holdem.ICardShuffler cardShuffler);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.WithCardShuffler(TabletopCardEngine.Holdem.ICardShuffler).cardShuffler'></a>

`cardShuffler` [ICardShuffler](TabletopCardEngine.Holdem.ICardShuffler.md 'TabletopCardEngine\.Holdem\.ICardShuffler')

The source of pre\-determined cards\.

#### Returns
[HoldemOptionsBuilder](TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.md 'TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder')  
The same builder, for chaining\.

<a name='TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.WithSettings(System.Action_TabletopCardEngine.Holdem.Options.HoldemSettings_)'></a>

## HoldemOptionsBuilder\.WithSettings\(Action&lt;HoldemSettings&gt;\) Method

Adjusts the validation limits applied to every game\.

```csharp
public TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder WithSettings(System.Action<TabletopCardEngine.Holdem.Options.HoldemSettings> configureSettings);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.WithSettings(System.Action_TabletopCardEngine.Holdem.Options.HoldemSettings_).configureSettings'></a>

`configureSettings` [System\.Action&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')[HoldemSettings](TabletopCardEngine.Holdem.Options.HoldemSettings.md 'TabletopCardEngine\.Holdem\.Options\.HoldemSettings')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.action-1 'System\.Action\`1')

Receives the settings to mutate\.

#### Returns
[HoldemOptionsBuilder](TabletopCardEngine.Holdem.Options.HoldemOptionsBuilder.md 'TabletopCardEngine\.Holdem\.Options\.HoldemOptionsBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.ArgumentOutOfRangeException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentoutofrangeexception 'System\.ArgumentOutOfRangeException')  
The settings end up with a minimum below 2, a maximum above 6 \(the v1 scope, which a setting
can only narrow\), or a minimum above the maximum\. That is a configuration error, not a game result\.
