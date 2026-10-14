#### [TabletopCardEngine](index.md 'index')
### [TabletopCardEngine](TabletopCardEngine.md 'TabletopCardEngine')

## TabletopCardEngineTelemetry Class

The telemetry sources declared by the Holdem engine for OpenTelemetry hosts\.

```csharp
public static class TabletopCardEngineTelemetry
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → TabletopCardEngineTelemetry
### Fields

<a name='TabletopCardEngine.TabletopCardEngineTelemetry.ActivitySourceName'></a>

## TabletopCardEngineTelemetry\.ActivitySourceName Field

The name of the [System\.Diagnostics\.ActivitySource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource 'System\.Diagnostics\.ActivitySource') the engine publishes to\.
Pass this to `TracerProviderBuilder.AddSource` when wiring up OpenTelemetry\.

```csharp
public const string ActivitySourceName = "TabletopCardEngine";
```

#### Field Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='TabletopCardEngine.TabletopCardEngineTelemetry.MeterName'></a>

## TabletopCardEngineTelemetry\.MeterName Field

The name of the [System\.Diagnostics\.Metrics\.Meter](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meter 'System\.Diagnostics\.Metrics\.Meter') the engine publishes to\.
Pass this to `MeterProviderBuilder.AddMeter` when wiring up OpenTelemetry\.

```csharp
public const string MeterName = "TabletopCardEngine";
```

#### Field Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
