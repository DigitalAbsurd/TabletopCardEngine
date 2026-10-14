#### [TabletopCardEngine\.SqlServer](index.md 'index')
### [TabletopCardEngine\.SqlServer](TabletopCardEngine.SqlServer.md 'TabletopCardEngine\.SqlServer')

## HoldemSqlServerDbContextOptionsBuilderExtensions Class

Configures the engine's database as SQL Server with the engine's own migrations in the `tce_holdem` schema\.
The plain `UseSqlServer` call is not enough: EF would look for migrations in the core assembly, which has
none, and [MigrateAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)') would fail with
[MigrationsNotFound](../TabletopCardEngine/TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\.

```csharp
public static class HoldemSqlServerDbContextOptionsBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemSqlServerDbContextOptionsBuilderExtensions
### Methods

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string)'></a>

## HoldemSqlServerDbContextOptionsBuilderExtensions\.UseHoldemSqlServer\(this DbContextOptionsBuilder, string\) Method

Uses the SQL Server database at [connectionString](TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.md#TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString 'TabletopCardEngine\.SqlServer\.HoldemSqlServerDbContextOptionsBuilderExtensions\.UseHoldemSqlServer\(this Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder, string\)\.connectionString'), with the migrations of this
package and the engine's own history table, `tce_holdem.__TceMigrationsHistory`\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemSqlServer(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, string connectionString);
```
#### Parameters

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString'></a>

`connectionString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A SQL Server connection string, e\.g\. `Server=.;Database=holdem;Trusted_Connection=True;TrustServerCertificate=True`\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection)'></a>

## HoldemSqlServerDbContextOptionsBuilderExtensions\.UseHoldemSqlServer\(this DbContextOptionsBuilder, DbConnection\) Method

Uses an already created SQL Server connection, with the migrations of this package and the
engine's own history table in the `tce_holdem` schema\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemSqlServer(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, System.Data.Common.DbConnection connection);
```
#### Parameters

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.SqlServer.HoldemSqlServerDbContextOptionsBuilderExtensions.UseHoldemSqlServer(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbConnection).connection'></a>

`connection` [System\.Data\.Common\.DbConnection](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbconnection 'System\.Data\.Common\.DbConnection')

A SQL Server connection the caller owns\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.
