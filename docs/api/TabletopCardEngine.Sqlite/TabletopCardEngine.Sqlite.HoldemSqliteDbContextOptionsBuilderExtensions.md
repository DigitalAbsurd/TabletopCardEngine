#### [TabletopCardEngine\.Sqlite](index.md 'index')
### [TabletopCardEngine\.Sqlite](TabletopCardEngine.Sqlite.md 'TabletopCardEngine\.Sqlite')

## HoldemSqliteDbContextOptionsBuilderExtensions Class

Configures the engine's database as SQLite with the engine's own migrations\. The plain
`UseSqlite` call is not enough: EF would look for migrations in the core assembly, which has
none, and [MigrateAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)') would fail with
[MigrationsNotFound](../TabletopCardEngine/TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\.

```csharp
public static class HoldemSqliteDbContextOptionsBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemSqliteDbContextOptionsBuilderExtensions
### Methods

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string)'></a>

## HoldemSqliteDbContextOptionsBuilderExtensions\.UseHoldemSqlite\(this DbContextOptionsBuilder, string\) Method

Uses the SQLite database at [connectionString](TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.md#TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString 'TabletopCardEngine\.Sqlite\.HoldemSqliteDbContextOptionsBuilderExtensions\.UseHoldemSqlite\(this Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder, string\)\.connectionString'), with the migrations of this
package and the engine's own history table, `tce_holdem___TceMigrationsHistory`\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemSqlite(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, string connectionString);
```
#### Parameters

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString'></a>

`connectionString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A SQLite connection string, e\.g\. `Data Source=holdem.db`\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection)'></a>

## HoldemSqliteDbContextOptionsBuilderExtensions\.UseHoldemSqlite\(this DbContextOptionsBuilder, DbConnection\) Method

Uses an already created SQLite connection, with the migrations of this package and the
engine's own history table\. This is the overload for an in\-memory database, which lives only
as long as its connection stays open, so the caller keeps it open and disposes of it\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemSqlite(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, System.Data.Common.DbConnection connection);
```
#### Parameters

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.Sqlite.HoldemSqliteDbContextOptionsBuilderExtensions.UseHoldemSqlite(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection).connection'></a>

`connection` [System\.Data\.Common\.DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection 'System\.Data\.Common\.DbConnection')

A SQLite connection the caller owns\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.
