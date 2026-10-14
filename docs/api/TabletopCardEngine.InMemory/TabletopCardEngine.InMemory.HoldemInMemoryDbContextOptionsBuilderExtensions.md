#### [TabletopCardEngine\.InMemory](index.md 'index')
### [TabletopCardEngine\.InMemory](TabletopCardEngine.InMemory.md 'TabletopCardEngine\.InMemory')

## HoldemInMemoryDbContextOptionsBuilderExtensions Class

Configures the engine's database as EF Core InMemory\.

```csharp
public static class HoldemInMemoryDbContextOptionsBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemInMemoryDbContextOptionsBuilderExtensions
### Methods

<a name='TabletopCardEngine.InMemory.HoldemInMemoryDbContextOptionsBuilderExtensions.UseHoldemInMemory(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string)'></a>

## HoldemInMemoryDbContextOptionsBuilderExtensions\.UseHoldemInMemory\(this DbContextOptionsBuilder, string\) Method

Uses an in\-memory database for the engine, with the given name as the store identifier\. Two
engines given the same name will share the same in\-memory store; use a unique name for each
isolated engine \(one per test, one per tenant, etc\.\)\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemInMemory(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, string databaseName);
```
#### Parameters

<a name='TabletopCardEngine.InMemory.HoldemInMemoryDbContextOptionsBuilderExtensions.UseHoldemInMemory(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.InMemory.HoldemInMemoryDbContextOptionsBuilderExtensions.UseHoldemInMemory(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).databaseName'></a>

`databaseName` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A unique name for this in\-memory store\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.

### Remarks
The InMemory provider is for prototyping, testing and isolated unit testing, not for
production\. It has four limits: state lives only in the process and is discarded on restart;
foreign keys are not enforced; transactions are ignored; and [System\.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal 'System\.Decimal') precision
is not applied \(although the engine's own two\-decimal rule is\)\. See the persistence
documentation for details\.
