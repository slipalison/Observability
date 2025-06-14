# Instrumentação OpenTelemetry para .NET

## Visão Geral

Este documento detalha como instrumentar aplicações .NET com OpenTelemetry para gerar telemetria completa (traces, métricas e logs).

## Pacotes Necessários

Adicione os seguintes pacotes NuGet ao seu projeto:

```xml
<ItemGroup>
  <!-- Pacotes principais do OpenTelemetry -->
  <PackageReference Include="OpenTelemetry" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.5.1" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Process" Version="0.5.0-beta.3" />

  <!-- Exportadores -->
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.7.0-rc.1" />

  <!-- Integração com Serilog -->
  <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="1.0.0" />
</ItemGroup>
```

## Configuração de Traces

Adicione ao seu `Program.cs`:

```csharp
// 1. Adicionar OpenTelemetry ao serviço
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            // Instrumentação automática
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("http.request.headers.user_agent", httpRequest.Headers.UserAgent);
                    activity.SetTag("http.request.query", httpRequest.QueryString.Value);
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("http.response.headers.content_type", httpResponse.Headers.ContentType);
                };
                options.EnrichWithException = (activity, exception) =>
                {
                    activity.SetTag("exception.message", exception.Message);
                    activity.SetTag("exception.stacktrace", exception.StackTrace);
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request.method", request.Method);
                    activity.SetTag("http.request.uri", request.RequestUri?.ToString());
                };
                options.EnrichWithHttpResponseMessage = (activity, response) =>
                {
                    activity.SetTag("http.response.status_code", (int)response.StatusCode);
                };
            })
            // Adicionar fonte personalizada
            .AddSource("Observability.WebApi")
            // Configurar Resource (contexto)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "observability-webapi", serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName,
                        ["host.name"] = Environment.MachineName,
                    })
            )
            // Exportador OTLP
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://otel-collector:4318");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
    });
```

## Configuração de Métricas

Adicione ao seu `Program.cs`:

```csharp
// 2. Configurar métricas
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            // Instrumentação automática
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            // Adicionar medidor personalizado
            .AddMeter("Observability.WebApi")
            // Configurar Resource (contexto)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "observability-webapi", serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName,
                        ["host.name"] = Environment.MachineName,
                    })
            )
            // Exportador OTLP
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://otel-collector:4318");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            })
            // Exportador Prometheus
            .AddPrometheusExporter();
    });

// 3. Registrar endpoint do Prometheus
app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

## Configuração de Logs (Serilog para OTLP)

Atualize sua configuração do Serilog:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    // Adicionar exportador OTLP
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://otel-collector:4318";
        options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "observability-webapi",
            ["service.version"] = "1.0.0",
            ["deployment.environment"] = builder.Environment.EnvironmentName
        };
    })
    .CreateLogger();
```

## Exemplo de Métricas Personalizadas

Crie uma classe para registrar métricas de negócio:

```csharp
using System.Diagnostics.Metrics;

namespace Observability.WebApi.Metrics;

public class WeatherMetrics
{
    private readonly Counter<long> _weatherRequestsCounter;
    private readonly Histogram<double> _temperatureHistogram;
    private readonly Counter<long> _summaryCategoriesCounter;

    private static readonly Meter _meter = new("Observability.WebApi");

    public WeatherMetrics()
    {
        // Contador de requisições de previsão do tempo
        _weatherRequestsCounter = _meter.CreateCounter<long>(
            "weather_forecast_requests_total",
            description: "Número total de requisições de previsão do tempo");

        // Histograma de temperaturas previstas
        _temperatureHistogram = _meter.CreateHistogram<double>(
            "weather_forecast_temperature_celsius",
            unit: "celsius",
            description: "Distribuição de temperaturas previstas");

        // Contador de categorias de clima
        _summaryCategoriesCounter = _meter.CreateCounter<long>(
            "weather_forecast_summary_total",
            description: "Contagem de previsões por categoria de resumo");
    }

    public void RecordWeatherRequest()
    {
        _weatherRequestsCounter.Add(1);
    }

    public void RecordTemperature(double temperatureCelsius, int dayOffset)
    {
        _temperatureHistogram.Record(temperatureCelsius, new KeyValuePair<string, object?>("day", dayOffset));
    }

    public void RecordSummaryCategory(string summary)
    {
        _summaryCategoriesCounter.Add(1, new KeyValuePair<string, object?>("summary", summary));
    }
}
```

## Uso no Controller

Instrumente seu controller:

```csharp
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly WeatherMetrics _metrics;
    private static readonly ActivitySource _activitySource = new("Observability.WebApi");

    public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> Get()
    {
        // Registrar métrica de requisição
        _metrics.RecordWeatherRequest();

        using var activity = _activitySource.StartActivity("GenerateWeatherForecast");
        activity?.SetTag("operation.name", "GetWeatherForecast");

        try
        {            
            var forecasts = Enumerable.Range(1, 5).Select(index => 
            {
                var temp = Random.Shared.Next(-20, 55);
                var summary = GetSummaryForTemperature(temp);

                // Registrar métricas de temperatura e resumo
                _metrics.RecordTemperature(temp, index);
                _metrics.RecordSummaryCategory(summary);

                return new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = temp,
                    Summary = summary
                };
            }).ToArray();

            _logger.LogInformation("Geradas {Count} previsões de tempo", forecasts.Length);
            activity?.SetTag("forecast.count", forecasts.Length);

            return Ok(forecasts);
        }
        catch (Exception ex)
        {            
            _logger.LogError(ex, "Erro ao gerar previsões do tempo");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return StatusCode(500, "Erro ao processar a requisição");
        }
    }

    private string GetSummaryForTemperature(int temperatureC)
    {
        return temperatureC switch
        {
            < 0 => "Freezing",
            < 10 => "Bracing",
            < 15 => "Chilly",
            < 20 => "Cool",
            < 25 => "Mild",
            < 30 => "Warm",
            < 35 => "Balmy",
            < 40 => "Hot",
            < 45 => "Sweltering",
            _ => "Scorching"
        };
    }
}
```

## Registrando o Serviço de Métricas

Adicione no `Program.cs`:

```csharp
// Registrar serviço de métricas como singleton
builder.Services.AddSingleton<WeatherMetrics>();
```

## Configuração no appsettings.json

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://otel-collector:4318"
  },
  // Resto da configuração...
}
```

## Próximos Passos

1. Instrumentar chamadas a banco de dados com `OpenTelemetry.Instrumentation.EntityFrameworkCore`
2. Instrumentar mensageria com `OpenTelemetry.Instrumentation.MassTransit` ou `OpenTelemetry.Instrumentation.RabbitMQ`
3. Implementar métricas de negócio personalizadas para KPIs específicos
4. Configurar alertas baseados em métricas e traces
