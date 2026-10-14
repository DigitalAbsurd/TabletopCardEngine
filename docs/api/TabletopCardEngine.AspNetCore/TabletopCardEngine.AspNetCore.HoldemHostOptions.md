#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemHostOptions Class

The engine settings a host can bind from configuration\. They are validated through the Options
pattern, so a bad value fails the host on start instead of throwing from service registration\.

The configuration section is the source of the engine's settings: it overwrites any
`WithSettings` call made inside the `configure` callback of
the `AddHoldem(configuration, ...)` overload in [HoldemHostingBuilderExtensions](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions').

```csharp
public sealed class HoldemHostOptions : System.ComponentModel.DataAnnotations.IValidatableObject
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemHostOptions

Implements [System\.ComponentModel\.DataAnnotations\.IValidatableObject](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject 'System\.ComponentModel\.DataAnnotations\.IValidatableObject')
### Properties

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.EnableSensitiveCardLogging'></a>

## HoldemHostOptions\.EnableSensitiveCardLogging Property

When `true`, the engine logs hole cards and the board at Trace level\. Never enable it in production\.

```csharp
public bool EnableSensitiveCardLogging { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.GamePlayersMaxCount'></a>

## HoldemHostOptions\.GamePlayersMaxCount Property

Most seats a game may have\. This release allows 2 to 6; a setting can only narrow it\.

```csharp
public int GamePlayersMaxCount { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.GamePlayersMinCount'></a>

## HoldemHostOptions\.GamePlayersMinCount Property

Fewest seats a game may have\. This release allows 2 to 6; a setting can only narrow it\.

```csharp
public int GamePlayersMinCount { get; set; }
```

#### Property Value
[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.MigrateDatabaseOnStartup'></a>

## HoldemHostOptions\.MigrateDatabaseOnStartup Property

When `true`, the hosted service migrates the database schema on start\-up, before any request reaches
the engine\. A failed migration throws out of `StartAsync` and fails the host\. Default is `false`,
so a deployment must opt in explicitly\.

```csharp
public bool MigrateDatabaseOnStartup { get; set; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')
### Methods

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.Validate(System.ComponentModel.DataAnnotations.ValidationContext)'></a>

## HoldemHostOptions\.Validate\(ValidationContext\) Method

Determines whether the specified object is valid\.

```csharp
public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext);
```
#### Parameters

<a name='TabletopCardEngine.AspNetCore.HoldemHostOptions.Validate(System.ComponentModel.DataAnnotations.ValidationContext).validationContext'></a>

`validationContext` [System\.ComponentModel\.DataAnnotations\.ValidationContext](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationcontext 'System\.ComponentModel\.DataAnnotations\.ValidationContext')

The validation context\.

Implements [Validate\(ValidationContext\)](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject.validate#system-componentmodel-dataannotations-ivalidatableobject-validate(system-componentmodel-dataannotations-validationcontext) 'System\.ComponentModel\.DataAnnotations\.IValidatableObject\.Validate\(System\.ComponentModel\.DataAnnotations\.ValidationContext\)')

#### Returns
[System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[System\.ComponentModel\.DataAnnotations\.ValidationResult](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationresult 'System\.ComponentModel\.DataAnnotations\.ValidationResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')  
A collection that holds failed\-validation information\.
