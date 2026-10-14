#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemHealthChecksBuilderExtensions Class

Adds the engine's database check to an ASP\.NET Core health checks registration\.

```csharp
public static class HoldemHealthChecksBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemHealthChecksBuilderExtensions
### Methods

<a name='TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_)'></a>

## HoldemHealthChecksBuilderExtensions\.AddHoldemCheck\(this IHealthChecksBuilder, string, Nullable&lt;HealthStatus&gt;, IEnumerable&lt;string&gt;\) Method

Registers a check that reads [GetSchemaStatusAsync\(CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemSchemaManager.md#TabletopCardEngine.Holdem.IHoldemSchemaManager.GetSchemaStatusAsync(System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemSchemaManager\.GetSchemaStatusAsync\(System\.Threading\.CancellationToken\)') on every
probe, so it reports both whether the database is reachable and whether its schema is one this
engine can serve\.

A schema that needs migrating, or one migrated by a newer engine, reports
[failureStatus](TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.md#TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_).failureStatus 'TabletopCardEngine\.AspNetCore\.HoldemHealthChecksBuilderExtensions\.AddHoldemCheck\(this Microsoft\.Extensions\.DependencyInjection\.IHealthChecksBuilder, string, System\.Nullable&lt;Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus&gt;, System\.Collections\.Generic\.IEnumerable&lt;string&gt;\)\.failureStatus'): the engine refuses every call in either state, so the default
is [Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Unhealthy](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthstatus.unhealthy 'Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Unhealthy') rather than [Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Degraded](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthstatus.degraded 'Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Degraded'). An
unreachable database surfaces as the provider's exception, which the health check service
reports with the same status. The check never migrates the database.

```csharp
public static Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder AddHoldemCheck(this Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder builder, string name="tabletopcardengine-holdem", System.Nullable<Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus> failureStatus=null, System.Collections.Generic.IEnumerable<string>? tags=null);
```
#### Parameters

<a name='TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_).builder'></a>

`builder` [Microsoft\.Extensions\.DependencyInjection\.IHealthChecksBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.ihealthchecksbuilder 'Microsoft\.Extensions\.DependencyInjection\.IHealthChecksBuilder')

The health checks builder returned by `AddHealthChecks()`\.

<a name='TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_).name'></a>

`name` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The registration name, which is also the key of the entry in the report\.

<a name='TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_).failureStatus'></a>

`failureStatus` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthstatus 'Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The status to report on failure; `null` means [Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Unhealthy](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthstatus.unhealthy 'Microsoft\.Extensions\.Diagnostics\.HealthChecks\.HealthStatus\.Unhealthy')\.

<a name='TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.AddHoldemCheck(thisMicrosoft.Extensions.DependencyInjection.IHealthChecksBuilder,string,System.Nullable_Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus_,System.Collections.Generic.IEnumerable_string_).tags'></a>

`tags` [System\.Collections\.Generic\.IEnumerable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1 'System\.Collections\.Generic\.IEnumerable\`1')

Tags a health endpoint can filter the check by\.

#### Returns
[Microsoft\.Extensions\.DependencyInjection\.IHealthChecksBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.ihealthchecksbuilder 'Microsoft\.Extensions\.DependencyInjection\.IHealthChecksBuilder')  
The same builder, for chaining\.
