using System.Diagnostics.Metrics;
using Observability.WebApi.Application.Orders;

namespace Observability.WebApi.Infrastructure.Metrics;

/// <summary>
/// Centraliza a criação e o gerenciamento dos instrumentos de métricas (Meters, Counters, etc.)
/// para o módulo de E-commerce. Isso garante consistência nos nomes e descrições
/// e facilita a injeção de dependência.
/// </summary>
public class ECommerceMetrics: IECommerceMetrics
{
    public const string MeterName = "Observability.WebApi.ECommerce";

    private readonly Meter _meter;

    /// <summary>
    /// Mede a duração das operações de banco de dados.
    /// É um Histograma para capturar a distribuição de latência.
    /// </summary>
    public Histogram<double> DatabaseOperationDuration { get; }

    /// <summary>
    /// Conta o valor monetário total dos pedidos.
    /// </summary>
    public Counter<double> OrdersValueTotal { get; }

    /// <summary>
    /// Conta o número de pedidos criados.
    /// </summary>
    public Counter<int> OrdersCreatedCount { get; }

    public ECommerceMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        DatabaseOperationDuration = _meter.CreateHistogram<double>(
            name: "app.db.operation.duration",
            unit: "s",
            description: "Measures the duration of database operations in seconds.");

        OrdersValueTotal = _meter.CreateCounter<double>(
            name: "app.orders.value.total",
            unit: "{USD}",
            description: "Tracks the total value of created orders.");

        OrdersCreatedCount = _meter.CreateCounter<int>(
            name: "app.orders.created.count",
            unit: "{orders}",
            description: "Counts the number of created orders.");
    }
}