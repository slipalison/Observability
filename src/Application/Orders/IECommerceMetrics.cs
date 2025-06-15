using System.Diagnostics.Metrics;

namespace Observability.WebApi.Application.Orders;

/// <summary>
/// Define o contrato para os instrumentos de métricas de E-commerce.
/// Esta abstração permite que a camada de aplicação registre métricas
/// sem depender de uma implementação concreta da infraestrutura.
/// </summary>
public interface IECommerceMetrics
{
    /// <summary>
    /// Mede a duração das operações de banco de dados.
    /// </summary>
    Histogram<double> DatabaseOperationDuration { get; }

    /// <summary>
    /// Conta o valor monetário total dos pedidos.
    /// </summary>
    Counter<double> OrdersValueTotal { get; }

    /// <summary>
    /// Conta o número de pedidos criados.
    /// </summary>
    Counter<int> OrdersCreatedCount { get; }
}