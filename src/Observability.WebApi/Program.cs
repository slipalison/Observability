using System.Diagnostics;
using System.Diagnostics.Metrics;
using Application.Orders;
using Npgsql;
using Observability.WebApi.Application.Orders;
using Observability.WebApi.Infrastructure.Metrics;
using Observability.WebApi.Infrastructure.Persistence;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;


namespace Observability.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 2. Logger bootstrap usando a mesma configuração do appsettings.json
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateBootstrapLogger();


        try
        {
            Log.Information("Iniciando aplicação Observability.WebApi");

            var builder = WebApplication.CreateBuilder(args);

            // 2. Configurar Serilog com base no appsettings.json
            builder.Host.UseSerilog((context, services, loggerConfig) =>
            {
                loggerConfig
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });

            OpenTelemetry(builder);
            
            

            // ==========================================================================================
            // 1. REGISTRO DOS SERVIÇOS NO CONTÊINER DE INJEÇÃO DE DEPENDÊNCIA
            // ==========================================================================================

            // Infraestrutura: Registra a fonte de dados do PostgreSQL como um singleton.
            // O NpgsqlDataSource gerencia o pool de conexões de forma eficiente.
            var connectionString = builder.Configuration.GetConnectionString("Database");
            builder.Services.AddSingleton<NpgsqlDataSource>(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

            // Infraestrutura -> Aplicação: Registra a implementação concreta (ECommerceMetrics)
            // e sua abstração (IECommerceMetrics). É um singleton para que a mesma instância
            // dos contadores/histogramas seja usada em toda a aplicação.
            builder.Services.AddSingleton<ECommerceMetrics>();
            builder.Services.AddSingleton<IECommerceMetrics>(sp => sp.GetRequiredService<ECommerceMetrics>());

            // Infraestrutura -> Aplicação: Registra a implementação do repositório.
            // Scoped significa que uma nova instância será criada para cada requisição HTTP.
            builder.Services.AddScoped<IOrderRepository, PostgresOrderRepository>();
            
            // Aplicação: Registra o serviço de aplicação.
            builder.Services.AddScoped<OrderService>();
            
            // ==========================================================================================


            
            
            // 3. Configuração de serviços
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Observability API",
                    Version = "v1",
                    Description = "API para demonstração de observabilidade"
                });
            });
            //builder.Services.AddOpenApi();

            var app = builder.Build();

            // 4. Middleware de logging com enriquecimento de contexto
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                    diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
                };
            });

            // 5. Swagger apenas em desenvolvimento
            if (app.Environment.IsDevelopment())
            {
                // app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Observability API v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            // 6. Middleware para tratamento de exceções
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature =
                        context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    Log.Error(exception, "Exceção não tratada: {ErrorMessage}", exception?.Message);

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { error = "Ocorreu um erro interno no servidor" });
                });
            });

            app.UseAuthorization();
            app.MapControllers();
            app.Run();

            Log.Information("Aplicação iniciada com sucesso");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "A aplicação terminou inesperadamente");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void OpenTelemetry(WebApplicationBuilder builder)
    {
        const string serviceName = "Observability.WebApi";
        const string serviceVersion = "1.0.0";
        const string otelCollectorEndpoint = "http://otel-collector:4318";


        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion))

            // TRACES - configuração
            .WithTracing(tracerBuilder => tracerBuilder
                // Instrumentações automáticas para frameworks
                .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddRedisInstrumentation()

                // Fontes de telemetria
                .AddSource("Serilog") // Integração com Serilog
                .AddSource(serviceName)

                // Fonte personalizada

                // Exportação para o OpenTelemetry Collector
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri($"{otelCollectorEndpoint}/v1/traces");
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
                .AddMeter(serviceName)
                .AddMeter(ECommerceMetrics.MeterName)

                // Exportação para o OpenTelemetry Collector
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri($"{otelCollectorEndpoint}/v1/metrics");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                })
            );

        builder.Services.AddSingleton(new ActivitySource(serviceName));

// ==========================================================================================
// 1. CRIE E REGISTRE O METER PARA INJEÇÃO DE DEPENDÊNCIA
// ==========================================================================================
        var meter = new Meter(serviceName);

// ESTA É A LINHA MAIS IMPORTANTE PARA CORRIGIR O ERRO:
        builder.Services.AddSingleton(meter);
// ==========================================================================================
    }
}