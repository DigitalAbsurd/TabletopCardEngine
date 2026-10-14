#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine\.Holdem](TabletopCardEngine.Holdem.md 'TabletopCardEngine\.Holdem')

## HoldemSchemaException Class

The database cannot be used as the engine is configured: a deployment fault, not a game result,
which is why it is an exception rather than a [HoldemErrorCode](TabletopCardEngine.Holdem.HoldemErrorCode.md 'TabletopCardEngine\.Holdem\.HoldemErrorCode')\.

```csharp
public sealed class HoldemSchemaException : System.InvalidOperationException
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception') → [System\.SystemException](https://learn.microsoft.com/en-us/dotnet/api/system.systemexception 'System\.SystemException') → [System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException') → HoldemSchemaException
### Constructors

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException()'></a>

## HoldemSchemaException\(\) Constructor

Creates an exception with no message and no status\.

```csharp
public HoldemSchemaException();
```

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException(string)'></a>

## HoldemSchemaException\(string\) Constructor

Creates an exception with a message and no status\.

```csharp
public HoldemSchemaException(string? message);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException(string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

What went wrong\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException(string,System.Exception)'></a>

## HoldemSchemaException\(string, Exception\) Constructor

Creates an exception with a message, the exception that caused it, and no status\.

```csharp
public HoldemSchemaException(string? message, System.Exception? innerException);
```
#### Parameters

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException(string,System.Exception).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

What went wrong\.

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.HoldemSchemaException(string,System.Exception).innerException'></a>

`innerException` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

The exception that caused this one\.
### Properties

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.Failure'></a>

## HoldemSchemaException\.Failure Property

Why the database cannot be used\. An exception built with one of the public constructors
carries `default`, which is not a defined member\.

```csharp
public TabletopCardEngine.Holdem.HoldemSchemaFailure Failure { get; }
```

#### Property Value
[HoldemSchemaFailure](TabletopCardEngine.Holdem.HoldemSchemaFailure.md 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure')

<a name='TabletopCardEngine.Holdem.HoldemSchemaException.Status'></a>

## HoldemSchemaException\.Status Property

The schema status that led to the failure, or `null` when it was not read\.

```csharp
public TabletopCardEngine.Holdem.HoldemSchemaStatus? Status { get; }
```

#### Property Value
[HoldemSchemaStatus](TabletopCardEngine.Holdem.HoldemSchemaStatus.md 'TabletopCardEngine\.Holdem\.HoldemSchemaStatus')
