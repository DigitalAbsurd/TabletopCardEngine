#### [TabletopCardEngine\.OpenTelemetry](index.md 'index')

## TabletopCardEngine\.OpenTelemetry Namespace

| Classes | |
| :--- | :--- |
| [TabletopCardEngineInstrumentationExtensions](TabletopCardEngine.OpenTelemetry.TabletopCardEngineInstrumentationExtensions.md 'TabletopCardEngine\.OpenTelemetry\.TabletopCardEngineInstrumentationExtensions') | Turns the engine's traces and metrics on in an OpenTelemetry host by name, so the host does not have to know [ActivitySourceName](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineTelemetry.md#TabletopCardEngine.TabletopCardEngineTelemetry.ActivitySourceName 'TabletopCardEngine\.TabletopCardEngineTelemetry\.ActivitySourceName') and [MeterName](../TabletopCardEngine/TabletopCardEngine.TabletopCardEngineTelemetry.md#TabletopCardEngine.TabletopCardEngineTelemetry.MeterName 'TabletopCardEngine\.TabletopCardEngineTelemetry\.MeterName')\. The engine's logs, traces and metrics are described in `docs/observability.md`\.   Logging needs no extension here: the engine logs through `ILogger` under categories that start with `TabletopCardEngine`, so a host enables that category in its logging configuration and, if it wants OTLP logs, adds the OpenTelemetry SDK's `AddOpenTelemetry()` logging provider. |
