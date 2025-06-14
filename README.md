# Observabilidade Completa - Projeto Enterprise

Este projeto implementa uma arquitetura completa de observabilidade usando OpenTelemetry como padrão central, com monitoramento de:

- **Traces** (rastreamento distribuído)
- **Métricas** (desempenho e negócio)
- **Logs** (estruturados e correlacionados)

## 🚀 Stack de Observabilidade

- **OpenTelemetry Collector**: Hub central de telemetria
- **Tempo**: Armazenamento e consulta de traces (substituto do Jaeger)
- **Prometheus**: Coleta e armazenamento de métricas
- **Grafana**: Dashboards unificados
- **Alloy**: Coleta unificada de logs (substituto do Promtail)
- **Redis e PostgreSQL**: Para demonstrar rastreamento em diferentes serviços

## 📋 Pré-requisitos

- Docker e Docker Compose
- .NET 9.0

## 🔧 Execução

```bash
# Iniciar a stack completa
docker-compose up -d

# Verificar logs
docker-compose logs -f

# Parar a stack
docker-compose down
```

## 📊 Acessando Interfaces

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Tempo**: http://localhost:3200
- **OpenTelemetry Collector**: http://localhost:8888/metrics
- **Alloy**: http://localhost:12345
- **Aplicação .NET**: http://localhost:5240

## 🧩 Arquitetura

```
┌──────────────┐     ┌───────────────┐     ┌───────────────┐
│  Aplicações  │────▶│  OTel         │────▶│  Backends     │
│  .NET        │     │  Collector    │     │  Observability│
└──────────────┘     └───────────────┘     └───────────────┘
       │                      │                     ▲
       │                      │                     │
       ▼                      ▼                     │
┌──────────────┐     ┌───────────────┐     ┌───────────────┐
│   Redis      │     │  Alloy        │────▶│   Grafana     │
│   PostgreSQL │     │  (logs)       │     │   Dashboards  │
└──────────────┘     └───────────────┘     └───────────────┘
```

## 📝 Configuração do Serilog

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .Enrich.WithThreadId()
    .Enrich.WithThreadName()
    .Enrich.WithProperty("ApplicationName", "Observability.WebApi")
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateLogger();
```

## 🔍 Próximos Passos

1. Instrumentar aplicação .NET com OpenTelemetry
2. Adicionar métricas de negócio customizadas
3. Criar dashboards específicos para cada componente
4. Implementar alertas baseados em thresholds

## 📚 Recursos

- [Documentação OpenTelemetry](https://opentelemetry.io/docs/)
- [OpenTelemetry para .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Grafana](https://grafana.com/docs/)
- [Prometheus](https://prometheus.io/docs/)
- [Tempo](https://grafana.com/docs/tempo/latest/)