
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Observability.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
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
            builder.Services.AddOpenApi();

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
                app.MapOpenApi();
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
                    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
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
}
