#### [TabletopCardEngine\.AspNetCore](index.md 'index')

## TabletopCardEngine\.AspNetCore Namespace

| Classes | |
| :--- | :--- |
| [HoldemCreateGameRequest](TabletopCardEngine.AspNetCore.HoldemCreateGameRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemCreateGameRequest') | Carries the optional parameters for a `POST /games` request\. |
| [HoldemEndpointRouteBuilderExtensions](TabletopCardEngine.AspNetCore.HoldemEndpointRouteBuilderExtensions.md 'TabletopCardEngine\.AspNetCore\.HoldemEndpointRouteBuilderExtensions') | Maps the engine's opt\-in REST API into an ASP\.NET Core host\. |
| [HoldemHealthChecksBuilderExtensions](TabletopCardEngine.AspNetCore.HoldemHealthChecksBuilderExtensions.md 'TabletopCardEngine\.AspNetCore\.HoldemHealthChecksBuilderExtensions') | Adds the engine's database check to an ASP\.NET Core health checks registration\. |
| [HoldemHostingBuilderExtensions](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions') | Registers Hold'em in an ASP\.NET Core host, with its settings bound from configuration\. |
| [HoldemHostOptions](TabletopCardEngine.AspNetCore.HoldemHostOptions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostOptions') | The engine settings a host can bind from configuration\. They are validated through the Options pattern, so a bad value fails the host on start instead of throwing from service registration\.   The configuration section is the source of the engine's settings: it overwrites any `WithSettings` call made inside the `configure` callback of the `AddHoldem(configuration, ...)` overload in [HoldemHostingBuilderExtensions](TabletopCardEngine.AspNetCore.HoldemHostingBuilderExtensions.md 'TabletopCardEngine\.AspNetCore\.HoldemHostingBuilderExtensions'). |
| [HoldemPlayRequest](TabletopCardEngine.AspNetCore.HoldemPlayRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemPlayRequest') | Carries the action for a `POST /games/{gameId}/actions` request\. |
| [HoldemStartGameRequest](TabletopCardEngine.AspNetCore.HoldemStartGameRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemStartGameRequest') | Carries the players for a `POST /games/{gameId}/start` request\. |
| [HoldemStartHandRequest](TabletopCardEngine.AspNetCore.HoldemStartHandRequest.md 'TabletopCardEngine\.AspNetCore\.HoldemStartHandRequest') | Carries the optional parameters for a `POST /games/{gameId}/hands` request\. |
