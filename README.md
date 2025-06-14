# Observabilidade Enterprise - Stack Completa

Este projeto implementa uma arquitetura completa de observabilidade de nível enterprise usando OpenTelemetry como padrão central, fornecendo monitoramento unificado dos três pilares:

- **Traces** (rastreamento distribuído entre serviços)
- **Métricas** (desempenho e indicadores de negócio)
- **Logs** (estruturados e correlacionados com traces)

> **Observabilidade** é a capacidade de compreender o estado interno de um sistema a partir de seus dados externos, permitindo responder as perguntas: "O que está acontecendo?", "Por que está acontecendo?" e "Onde está o problema?".

## 🚀 Stack de Observabilidade

### Componentes Principais

- **OpenTelemetry Collector**: Hub central que recebe, processa e exporta todos os dados de telemetria
- **Grafana Tempo**: Armazenamento e consulta de traces distribuídos (substituto moderno do Jaeger)
- **Prometheus**: Sistema de monitoramento para coleta e armazenamento de métricas com modelo dimensional
- **Loki**: Sistema de agregação de logs inspirado no Prometheus, com indexação baseada em labels
- **Grafana**: Plataforma de visualização unificada para dashboards, análises e alertas
- **Grafana Alloy**: Agente de observabilidade unificado da Grafana Labs (substituto do Promtail)

### Bancos de Dados para Demonstração

- **Redis**: Cache em memória para demonstrar traces entre aplicação e cache
- **PostgreSQL**: Banco de dados relacional para demonstrar traces de consultas SQL

### Exporters e Instrumentação

- **Redis Exporter**: Coleta métricas específicas do Redis
- **PostgreSQL Exporter**: Coleta métricas específicas do PostgreSQL
- **OpenTelemetry .NET SDK**: Instrumentação automática e manual para aplicações .NET

### Versões Utilizadas

- OpenTelemetry Collector: 0.91.0
- Grafana Tempo: 2.3.1
- Prometheus: 2.48.1
- Loki: 2.9.3
- Grafana: 10.2.3
- Grafana Alloy: 1.0.0

## 📋 Pré-requisitos

### Software Necessário

- **Docker**: 20.10.0 ou superior
- **Docker Compose**: v2.0.0 ou superior (formato de arquivo Compose V2)
- **.NET**: SDK 9.0 ou superior

### Requisitos de Hardware (Mínimos)

- 4GB de RAM disponível para os containers
- 10GB de espaço em disco para armazenamento de dados

### Portas Utilizadas

- **3000**: Grafana Web UI
- **3100**: Loki API
- **3200**: Tempo API
- **4317/4318**: OpenTelemetry Collector (gRPC/HTTP)
- **5240/5241**: Aplicação .NET (API/Métricas)
- **8888**: OpenTelemetry Collector (métricas)
- **9090**: Prometheus Web UI
- **12345**: Alloy (status)
- **6379**: Redis
- **5432**: PostgreSQL

## 🔧 Execução

### Comandos Básicos

```bash
# Construir e iniciar a stack completa
docker-compose up -d --build

# Verificar logs de todos os serviços
docker-compose logs -f

# Verificar logs de um serviço específico
docker-compose logs -f grafana
docker-compose logs -f alloy
docker-compose logs -f otel-collector

# Reiniciar um serviço específico
docker-compose restart alloy

# Parar a stack mantendo volumes (dados persistidos)
docker-compose down

# Parar a stack e remover volumes (limpar completamente)
docker-compose down -v

# Verificar status dos serviços
docker-compose ps
```

### Verificando a Saúde dos Serviços

```bash
# Testar a API e gerar traces/logs
curl http://localhost:5240/WeatherForecast

# Verificar se o Prometheus está coletando alvos
curl http://localhost:9090/api/v1/targets | jq

# Verificar se o Loki está recebendo logs
curl "http://localhost:3100/loki/api/v1/labels" | jq

# Verificar os valores de um label específico no Loki
curl "http://localhost:3100/loki/api/v1/label/service/values" | jq

# Verificar a saúde do Tempo
curl http://localhost:3200/status
```

## 📊 Acessando Interfaces

### Dashboard Principal

- **Grafana**: http://localhost:3000 
  - Credenciais: admin/admin123
  - Datasources pré-configurados: Prometheus, Tempo e Loki
  - Para análise exploratória unificada: `/explore`
  - Para dashboards pré-configurados: `/dashboards`

### Sistemas de Observabilidade

- **Prometheus**: http://localhost:9090
  - Consultas PromQL: `/graph`
  - Status dos targets: `/targets`
  - Configuração: `/config`

- **Loki**: http://localhost:3100
  - API para consulta de logs
  - Status: `/ready`
  - Listagem de labels: `/loki/api/v1/labels`

- **Tempo**: http://localhost:3200
  - API para consulta de traces
  - Pesquisa por trace ID: `/tempo/api/traces/{trace-id}`

### Collectors e Agentes

- **OpenTelemetry Collector**: 
  - Métricas internas: http://localhost:8888/metrics
  - Health check: http://localhost:13133
  - Endpoints de recepção: 
    - gRPC: http://localhost:4317
    - HTTP: http://localhost:4318

- **Alloy (Grafana Agent)**: 
  - Status e métricas: http://localhost:12345

### Aplicação de Demonstração

- **Aplicação .NET**: 
  - API: http://localhost:5240
  - Métricas: http://localhost:5241

## 🧩 Arquitetura

```
┌───────────────────────────────────────────────────────────────────┐
│                     APLICAÇÕES INSTRUMENTADAS                      │
│                                                                   │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐           │
│  │  ASP.NET     │   │  Workers     │   │  Console     │           │
│  │  WebAPI      │   │  Services    │   │  Apps        │           │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘           │
│         │                  │                  │                    │
└─────────┼──────────────────┼──────────────────┼────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌───────────────────────────────────────────────────────────────────┐
│                      COLETA DE TELEMETRIA                         │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │                 OpenTelemetry Collector                  │     │
│  └──────┬───────────────────┬───────────────────┬───────────┘     │
│         │                   │                   │                  │
└─────────┼───────────────────┼───────────────────┼──────────────────┘
          │                   │                   │
          ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│    TRACES       │  │    MÉTRICAS     │  │      LOGS        │
│                 │  │                 │  │                  │
│  ┌───────────┐  │  │  ┌───────────┐  │  │  ┌───────────┐  │
│  │   Tempo   │  │  │  │Prometheus │  │  │  │   Loki    │  │
│  └─────┬─────┘  │  │  └─────┬─────┘  │  │  └─────┬─────┘  │
│        │        │  │        │        │  │        │        │
└────────┼────────┘  └────────┼────────┘  └────────┼────────┘
         │                    │                     │
         └────────────┬───────┴─────────────┬──────┘
                      │                     │
                      ▼                     ▼
             ┌─────────────────────────────────────┐
             │             VISUALIZAÇÃO            │
             │                                     │
             │            ┌───────────┐            │
             │            │  Grafana  │            │
             │            └───────────┘            │
             │                                     │
             └─────────────────────────────────────┘
                              ▲
                              │
┌────────────────────────────┴─────────────────────────────┐
│                     SERVIÇOS MONITORADOS                  │
│                                                           │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐  │
│  │   Redis      │   │  PostgreSQL  │   │    Alloy     │  │
│  │   + Exporter │   │  + Exporter  │   │ (Docker logs)│  │
│  └──────────────┘   └──────────────┘   └──────────────┘  │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

## 📝 Instrumentação e Configuração

### Configuração do Serilog para Logs Estruturados

Utilize o Serilog para gerar logs estruturados em JSON que serão coletados pelo Alloy e enviados ao Loki:

```csharp
// Pacotes NuGet necessários:
// - Serilog.AspNetCore
// - Serilog.Enrichers.Environment
// - Serilog.Enrichers.Thread
// - Serilog.Sinks.Console

Log.Logger = new LoggerConfiguration()
    // Níveis de log
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)

    // Enriquecimento com contexto e metadados
    .Enrich.FromLogContext()            // Contexto da operação
    .Enrich.WithMachineName()           // Nome da máquina
    .Enrich.WithEnvironmentName()       // Ambiente (dev, staging, prod)
    .Enrich.WithProcessId()             // ID do processo
    .Enrich.WithProcessName()           // Nome do processo
    .Enrich.WithThreadId()              // ID da thread
    .Enrich.WithThreadName()            // Nome da thread

    // Propriedades personalizadas
    .Enrich.WithProperty("ApplicationName", "Observability.WebApi")
    .Enrich.WithProperty("ApplicationVersion", "1.0.0")

    // IMPORTANTE: Formatação JSON para o console
    // Isso garante que o Alloy capture logs estruturados
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateLogger();
```

### Configuração do OpenTelemetry (Traces e Métricas)

```csharp
// Pacotes NuGet necessários:
// - OpenTelemetry.Extensions.Hosting
// - OpenTelemetry.Instrumentation.AspNetCore
// - OpenTelemetry.Instrumentation.Http
// - OpenTelemetry.Instrumentation.SqlClient
// - OpenTelemetry.Instrumentation.Runtime
// - OpenTelemetry.Exporter.OpenTelemetryProtocol
// - OpenTelemetry.Instrumentation.StackExchangeRedis

builder.Services.AddOpenTelemetry()
    // TRACES - configuração
    .WithTracing(tracerBuilder => tracerBuilder
        // Instrumentações automáticas para frameworks
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddRedisInstrumentation()

        // Fontes de telemetria
        .AddSource("Serilog")                    // Integração com Serilog
        .AddSource("Observability.WebApi")       // Fonte personalizada

        // Exportação para o OpenTelemetry Collector
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://otel-collector:4318");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        })
    )

    // MÉTRICAS - configuração
    .WithMetrics(metricsBuilder => metricsBuilder
        // Instrumentações automáticas para métricas
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()

        // Métricas personalizadas
        .AddMeter("Observability.WebApi")

        // Exportação para o OpenTelemetry Collector
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://otel-collector:4318");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        })
    );
```

## 🏷️ Labels e Metadados

### Configuração de Labels no Docker Compose

Adicione labels nos seus serviços no `compose.yaml` para categorizar seus containers:

```yaml
observability.webapi:
  # ... outras configurações ...
  labels:
    # Labels ESSENCIAIS para o Alloy
    - "logging=promtail"              # ← OBRIGATÓRIO para coleta de logs
    - "service=webapi"                # ← Nome do serviço
    - "team=platform"                 # ← Time responsável

    # Labels EXTRAS para melhor categorização
    - "application=observability"     # ← Nome da aplicação
    - "component=api"                 # ← Tipo do componente
    - "framework=dotnet"              # ← Framework usado
    - "version=1.0.0"                 # ← Versão da aplicação
    - "environment=development"       # ← Ambiente
    - "tier=backend"                  # ← Camada da aplicação
```

### Configuração do Alloy para Processar Labels

No arquivo `observability/alloy-config.alloy`, configure o processamento de labels:

```alloy
// Descoberta de containers Docker
discovery.docker "containers" {
    host = "unix:///var/run/docker.sock"
    refresh_interval = "5s"
}

// Processamento para extrair TODAS as labels
discovery.relabel "docker_labels" {
    targets = discovery.docker.containers.targets

    // Nome do container
    rule {
        source_labels = ["__meta_docker_container_name"]
        regex = "/(.*)"
        target_label = "container_name"
    }

    // Service do compose
    rule {
        source_labels = ["__meta_docker_container_label_com_docker_compose_service"]
        target_label = "compose_service"
    }

    // Labels personalizadas do compose.yaml
    rule {
        source_labels = ["__meta_docker_container_label_service"]
        target_label = "service"
    }

    rule {
        source_labels = ["__meta_docker_container_label_team"]
        target_label = "team"
    }

    // ... outras regras para cada label ...

    // IMPORTANTE: Filtrar apenas containers com logging=promtail
    rule {
        source_labels = ["__meta_docker_container_label_logging"]
        regex = "promtail"
        action = "keep"
    }
}

// Coleta de logs com targets processados
loki.source.docker "containers" {
    host = "unix:///var/run/docker.sock"
    targets = discovery.relabel.docker_labels.output
    forward_to = [loki.write.default.receiver]
}
```

### Consultas no Grafana com Labels

Utilize os labels para consultas poderosas no Grafana:

```
# Filtrar por aplicação e componente
{application="observability", component="api"}

# Filtrar por time e framework  
{team="platform", framework="dotnet"}

# Filtrar por ambiente e tier
{env="development", tier="backend"}

# Buscar texto em logs específicos
{service="webapi"} |= "Exception"

# Combinações complexas
{service="webapi", team="platform"} |= "WeatherForecast" | json | app_version="1.0.0"
```

## 🔍 Próximos Passos

1. Instrumentar aplicação .NET com OpenTelemetry
2. Adicionar métricas de negócio customizadas
3. Criar dashboards específicos para cada componente
4. Implementar alertas baseados em thresholds

## 🔄 Configuração de Datasources do Grafana

O Grafana é configurado automaticamente com datasources para todos os backends. O arquivo `observability/grafana/provisioning/datasources/datasources.yml` contém:

```yaml
datasources:
  # Prometheus - Para métricas
  - name: Prometheus
    type: prometheus
    uid: prometheus
    access: proxy
    url: http://prometheus:9090
    jsonData:
      exemplarTraceIdDestinations:
        # Link para traces
        - datasourceUid: tempo
          name: TraceID
      httpMethod: POST
      timeInterval: 15s

  # Tempo - Para traces distribuídos
  - name: Tempo
    type: tempo
    uid: tempo
    access: proxy
    url: http://tempo:3200
    jsonData:
      nodeGraph:
        enabled: true
      serviceMap:
        datasourceUid: prometheus
      tracesToLogs:
        datasourceUid: loki
        filterByTraceID: true
        spanStartTimeShift: '-1h'
        spanEndTimeShift: '1h'
        tags: ["service", "job"]
      search:
        hide: false
      lokiSearch:
        datasourceUid: loki

  # Loki - Para logs estruturados
  - name: Loki
    type: loki
    uid: loki
    access: proxy
    url: http://loki:3100
    jsonData:
      derivedFields:
        - datasourceUid: tempo
          matcherRegex: "traceId[\"=: ]+([a-zA-Z0-9]+)"
          name: TraceID
          url: "$${__value.raw}"
      maxLines: 1000
```

## 🔧 Troubleshooting

### Problemas Comuns

**Logs não aparecem no Loki**
- Verifique se seu container tem o label `logging=promtail`
- Verifique se o Alloy está rodando: `docker-compose ps alloy`
- Verifique os logs do Alloy: `docker-compose logs alloy`
- Teste a conexão com Loki: `curl http://localhost:3100/ready`

**Traces não aparecem no Tempo**
- Verifique se o OpenTelemetry Collector está acessível: `curl http://localhost:8888/metrics`
- Verifique se a configuração do exportador OTLP está correta (endpoint e porta)
- Gere tráfego na aplicação: `curl http://localhost:5240/WeatherForecast`

**Erro no Alloy: "missing ',' in field list"**
- A sintaxe do arquivo alloy-config.alloy está incorreta
- Verifique a formatação e vírgulas nas seções de regras

**Datasource não conecta no Grafana**
- Verifique se os serviços estão rodando: `docker-compose ps`
- Teste acesso direto às APIs: `curl http://localhost:9090/api/v1/status/config`
- Verifique se as URLs nos datasources apontam para os nomes corretos dos containers

## 📚 Recursos

- [Documentação OpenTelemetry](https://opentelemetry.io/docs/)
- [OpenTelemetry para .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [Grafana](https://grafana.com/docs/)
- [Prometheus](https://prometheus.io/docs/)
- [Tempo](https://grafana.com/docs/tempo/latest/)
- [Loki](https://grafana.com/docs/loki/latest/)
- [Alloy](https://grafana.com/docs/alloy/latest/)

## 🏁 Conclusão

Esta stack de observabilidade proporciona uma solução completa e enterprise-grade para monitoramento e diagnóstico de aplicações distribuídas. Utilizando os três pilares da observabilidade (logs, métricas e traces) de forma integrada, é possível:

- Identificar e resolver problemas rapidamente
- Monitorar performance em tempo real
- Correlacionar eventos entre diferentes serviços
- Analisar tendências e comportamentos
- Melhorar a experiência do usuário
- Reduzir tempo de inatividade
- Otimizar recursos e custos

A implementação da observabilidade não é apenas uma prática recomendada, mas um requisito essencial para sistemas modernos distribuídos e orientados a microserviços.

---

*Desenvolvido por Eduardo Pires - desenvolvedor.io*