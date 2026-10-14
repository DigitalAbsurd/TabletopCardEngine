#### [TabletopCardEngine\.PostgreSql](index.md 'index')
### [TabletopCardEngine\.PostgreSql](TabletopCardEngine.PostgreSql.md 'TabletopCardEngine\.PostgreSql')

## HoldemPostgreSqlDbContextOptionsBuilderExtensions Class

Configures the engine's database as PostgreSQL with the engine's own migrations in the `tce_holdem` schema\.
The plain `UseNpgsql` call is not enough: EF would look for migrations in the core assembly, which has
none, and [MigrateAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.MigrateAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.MigrateAsync\(System\.Threading\.CancellationToken\)') would fail with
[MigrationsNotFound](../TabletopCardEngine/TabletopCardEngine.Holdem.HoldemSchemaFailure.md#TabletopCardEngine.Holdem.HoldemSchemaFailure.MigrationsNotFound 'TabletopCardEngine\.Holdem\.HoldemSchemaFailure\.MigrationsNotFound')\.

```csharp
public static class HoldemPostgreSqlDbContextOptionsBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemPostgreSqlDbContextOptionsBuilderExtensions
### Methods

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string)'></a>

## HoldemPostgreSqlDbContextOptionsBuilderExtensions\.UseHoldemPostgreSql\(this DbContextOptionsBuilder, string\) Method

Uses the PostgreSQL database at [connectionString](TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.md#TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString 'TabletopCardEngine\.PostgreSql\.HoldemPostgreSqlDbContextOptionsBuilderExtensions\.UseHoldemPostgreSql\(this Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder, string\)\.connectionString'), with the migrations of this
package and the engine's own history table, `tce_holdem.__TceMigrationsHistory`\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemPostgreSql(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, string connectionString);
```
#### Parameters

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,string).connectionString'></a>

`connectionString` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

A PostgreSQL connection string, e\.g\. `Host=localhost;Database=holdem`\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbDataSource)'></a>

## HoldemPostgreSqlDbContextOptionsBuilderExtensions\.UseHoldemPostgreSql\(this DbContextOptionsBuilder, DbDataSource\) Method

Uses an already created PostgreSQL data source, with the migrations of this package and the
engine's own history table in the `tce_holdem` schema\. This is the recommended form for Npgsql,
allowing for connection pooling and advanced configuration\.

```csharp
public static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder UseHoldemPostgreSql(this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, System.Data.Common.DbDataSource dataSource);
```
#### Parameters

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbDataSource).builder'></a>

`builder` [Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')

The options passed to `AddHoldem`\.

<a name='TabletopCardEngine.PostgreSql.HoldemPostgreSqlDbContextOptionsBuilderExtensions.UseHoldemPostgreSql(thisMicrosoft.EntityFrameworkCore.DbContextOptionsBuilder,System.Data.Common.DbDataSource).dataSource'></a>

`dataSource` [System\.Data\.Common\.DbDataSource](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatasource 'System\.Data\.Common\.DbDataSource')

A PostgreSQL data source the caller owns\.

#### Returns
[Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder 'Microsoft\.EntityFrameworkCore\.DbContextOptionsBuilder')  
The same builder, for chaining\.
