#### [TabletopCardEngine\.AspNetCore](index.md 'index')
### [TabletopCardEngine\.AspNetCore](TabletopCardEngine.AspNetCore.md 'TabletopCardEngine\.AspNetCore')

## HoldemEndpointRouteBuilderExtensions Class

Maps the engine's opt\-in REST API into an ASP\.NET Core host\.

```csharp
public static class HoldemEndpointRouteBuilderExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → HoldemEndpointRouteBuilderExtensions
### Methods

<a name='TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.MapHoldemApi(thisMicrosoft.AspNetCore.Routing.IEndpointRouteBuilder,string)'></a>

## HoldemEndpointRouteBuilderExtensions\.MapHoldemApi\(this IEndpointRouteBuilder, string\) Method

Maps the engine's REST API under [prefix](TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.md#TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.MapHoldemApi(thisMicrosoft.AspNetCore.Routing.IEndpointRouteBuilder,string).prefix 'TabletopCardEngine\.AspNetCore\.HoldemEndpointRouteBuilderExtensions\.MapHoldemApi\(this Microsoft\.AspNetCore\.Routing\.IEndpointRouteBuilder, string\)\.prefix') and returns the group, so the host can chain
`RequireAuthorization()`, rate limiting and the like onto every route\. Nothing is mapped unless this is called\.

The API returns every seat's hole cards, as [GameData](../TabletopCardEngine/TabletopCardEngine.Holdem.Dto.GameData.md 'TabletopCardEngine\.Holdem\.Dto\.GameData') does: protect the group.

```csharp
public static Microsoft.AspNetCore.Routing.RouteGroupBuilder MapHoldemApi(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string prefix="/api/holdem");
```
#### Parameters

<a name='TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.MapHoldemApi(thisMicrosoft.AspNetCore.Routing.IEndpointRouteBuilder,string).endpoints'></a>

`endpoints` [Microsoft\.AspNetCore\.Routing\.IEndpointRouteBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.iendpointroutebuilder 'Microsoft\.AspNetCore\.Routing\.IEndpointRouteBuilder')

The route builder to map into\.

<a name='TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.MapHoldemApi(thisMicrosoft.AspNetCore.Routing.IEndpointRouteBuilder,string).prefix'></a>

`prefix` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The path the routes are grouped under\.

#### Returns
[Microsoft\.AspNetCore\.Routing\.RouteGroupBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.routegroupbuilder 'Microsoft\.AspNetCore\.Routing\.RouteGroupBuilder')  
The route group holding every route of the API\.

#### Exceptions

[System\.InvalidOperationException](https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception 'System\.InvalidOperationException')  
The services were not registered with `AddHoldem(configuration, ...)` of this package\.
