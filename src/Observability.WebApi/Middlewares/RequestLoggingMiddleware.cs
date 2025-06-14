using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using Serilog.Context;

namespace Observability.WebApi.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        // Adicionar Correlation ID ao response header
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Enriquecer o contexto de logging com informações da requisição
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
        using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString()))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("RemoteIp", GetClientIpAddress(context)))
        using (LogContext.PushProperty("Protocol", context.Request.Protocol))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Scheme", context.Request.Scheme))
        using (LogContext.PushProperty("Host", context.Request.Host.ToString()))
        using (LogContext.PushProperty("Path", context.Request.Path.ToString()))
        using (LogContext.PushProperty("QueryString", context.Request.QueryString.ToString()))
        using (LogContext.PushProperty("ContentType", context.Request.ContentType))
        using (LogContext.PushProperty("ContentLength", context.Request.ContentLength))
        using (LogContext.PushProperty("UserId", GetUserId(context)))
        using (LogContext.PushProperty("SessionId", GetSessionId(context)))
        {
            Log.Information("Requisição HTTP {Method} {Url} iniciada", 
                context.Request.Method, 
                context.Request.GetDisplayUrl());

            try
            {
                await _next(context);
                sw.Stop();

                // Adicionar informações de resposta
                using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                using (LogContext.PushProperty("ElapsedMilliseconds", sw.ElapsedMilliseconds))
                using (LogContext.PushProperty("ResponseContentType", context.Response.ContentType))
                using (LogContext.PushProperty("ResponseContentLength", context.Response.ContentLength))
                {
                    // Log adequado com base no status code
                    var logLevel = GetLogLevel(context.Response.StatusCode);
                    
                    Log.Write(logLevel,
                        "Requisição HTTP {Method} {Url} finalizada em {ElapsedMilliseconds}ms com status {StatusCode}",
                        context.Request.Method,
                        context.Request.GetDisplayUrl(),
                        sw.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                using (LogContext.PushProperty("ElapsedMilliseconds", sw.ElapsedMilliseconds))
                using (LogContext.PushProperty("ExceptionType", ex.GetType().Name))
                using (LogContext.PushProperty("ExceptionSource", ex.Source))
                {
                    Log.Error(ex, 
                        "Requisição HTTP {Method} {Url} falhou após {ElapsedMilliseconds}ms com exceção {ExceptionType}",
                        context.Request.Method,
                        context.Request.GetDisplayUrl(),
                        sw.ElapsedMilliseconds,
                        ex.GetType().Name);
                }

                throw;
            }
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Tentar obter IP real considerando proxies/load balancers
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static string? GetUserId(HttpContext context)
    {
        // Extrair User ID se autenticado
        return context.User?.Identity?.IsAuthenticated == true 
            ? context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value 
            : null;
    }

    private static string? GetSessionId(HttpContext context)
    {
        // Extrair Session ID se disponível
        return context.Session?.Id;
    }

    private static Serilog.Events.LogEventLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => Serilog.Events.LogEventLevel.Error,
            >= 400 => Serilog.Events.LogEventLevel.Warning,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }
}

