#### [TabletopCardEngine\.OpenTelemetry](index.md 'index')
### [TabletopCardEngine\.OpenTelemetry](TabletopCardEngine.OpenTelemetry.md 'TabletopCardEngine\.OpenTelemetry')

## TabletopCardEngineInstrumentationExtensions Class

Turns the engine's traces and metrics on in an OpenTelemetry host by name, so the host does not
have to know [ActivitySourceName](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineTelemetry.md#TabletopCardEngine.TabletopCardEngineTelemetry.ActivitySourceName 'TabletopCardEngine\.TabletopCardEngineTelemetry\.ActivitySourceName') and
[MeterName](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineTelemetry.md#TabletopCardEngine.TabletopCardEngineTelemetry.MeterName 'TabletopCardEngine\.TabletopCardEngineTelemetry\.MeterName')\. The engine's logs, traces and metrics are described
in `docs/observability.md`\.

Logging needs no extension here: the engine logs through `ILogger` under categories that
start with `TabletopCardEngine`, so a host enables that category in its logging configuration
and, if it wants OTLP logs, adds the OpenTelemetry SDK's `AddOpenTelemetry()` logging
provider.

```csharp
public static class TabletopCardEngineInstrumentationExtensions
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TabletopCardEngineInstrumentationExtensions
### Methods

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.IOpenTelemetryBuilder)'></a>

## TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this IOpenTelemetryBuilder\) Method

Subscribes both the tracer provider and the meter provider to the engine's
`ActivitySource` and `Meter` in a single call\. The host wires both signals with:

```csharp
services.AddOpenTelemetry().AddTabletopCardEngineInstrumentation()
```
chained with `WithTracing()` and `WithMetrics()` to add exporters\. The engine's
telemetry is described in `docs/observability.md`\.

```csharp
public static OpenTelemetry.IOpenTelemetryBuilder AddTabletopCardEngineInstrumentation(this OpenTelemetry.IOpenTelemetryBuilder builder);
```
#### Parameters

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.IOpenTelemetryBuilder).builder'></a>

`builder` [IOpenTelemetryBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api.ProviderBuilderExtensions/IOpenTelemetryBuilder.cs 'OpenTelemetry\.IOpenTelemetryBuilder')

The OpenTelemetry builder of the host\.

#### Returns
[IOpenTelemetryBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api.ProviderBuilderExtensions/IOpenTelemetryBuilder.cs 'OpenTelemetry\.IOpenTelemetryBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
[builder](TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.md#TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.IOpenTelemetryBuilder).builder 'TabletopCardEngine\.OpenTelemetry\.TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this OpenTelemetry\.IOpenTelemetryBuilder\)\.builder') is [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Metrics.MeterProviderBuilder)'></a>

## TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this MeterProviderBuilder\) Method

Subscribes the meter provider to the engine's `Meter`: the action, hand and
validation\-failure counters and the action duration histogram\. The engine's telemetry is
described in `docs/observability.md`\.

```csharp
public static OpenTelemetry.Metrics.MeterProviderBuilder AddTabletopCardEngineInstrumentation(this OpenTelemetry.Metrics.MeterProviderBuilder builder);
```
#### Parameters

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Metrics.MeterProviderBuilder).builder'></a>

`builder` [MeterProviderBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api/Metrics/MeterProviderBuilder.cs 'OpenTelemetry\.Metrics\.MeterProviderBuilder')

The meter provider builder of the host\.

#### Returns
[MeterProviderBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api/Metrics/MeterProviderBuilder.cs 'OpenTelemetry\.Metrics\.MeterProviderBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
[builder](TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.md#TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Metrics.MeterProviderBuilder).builder 'TabletopCardEngine\.OpenTelemetry\.TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this OpenTelemetry\.Metrics\.MeterProviderBuilder\)\.builder') is [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Trace.TracerProviderBuilder)'></a>

## TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this TracerProviderBuilder\) Method

Subscribes the tracer provider to the engine's `ActivitySource`, which opens one
`TabletopCardEngine.ApplyHoldemAction` span per call to
[ApplyAsync\(HoldemAction, CancellationToken\)](../TabletopCardEngine/TabletopCardEngine.Holdem.IHoldemTable.md#TabletopCardEngine.Holdem.IHoldemTable.ApplyAsync(TabletopCardEngine.Holdem.Dto.HoldemAction,System.Threading.CancellationToken) 'TabletopCardEngine\.Holdem\.IHoldemTable\.ApplyAsync\(TabletopCardEngine\.Holdem\.Dto\.HoldemAction,System\.Threading\.CancellationToken\)')\. The engine's telemetry is described in
`docs/observability.md`\.

```csharp
public static OpenTelemetry.Trace.TracerProviderBuilder AddTabletopCardEngineInstrumentation(this OpenTelemetry.Trace.TracerProviderBuilder builder);
```
#### Parameters

<a name='TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Trace.TracerProviderBuilder).builder'></a>

`builder` [TracerProviderBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api/Trace/TracerProviderBuilder.cs 'OpenTelemetry\.Trace\.TracerProviderBuilder')

The tracer provider builder of the host\.

#### Returns
[TracerProviderBuilder](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api/Trace/TracerProviderBuilder.cs 'OpenTelemetry\.Trace\.TracerProviderBuilder')  
The same builder, for chaining\.

#### Exceptions

[System\.ArgumentNullException](https://learn.microsoft.com/en-us/dotnet/api/system.argumentnullexception 'System\.ArgumentNullException')  
[builder](TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.md#TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.AddTabletopCardEngineInstrumentation(thisOpenTelemetry.Trace.TracerProviderBuilder).builder 'TabletopCardEngine\.OpenTelemetry\.TabletopCardEngineInstrumentationExtensions\.AddTabletopCardEngineInstrumentation\(this OpenTelemetry\.Trace\.TracerProviderBuilder\)\.builder') is [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.
