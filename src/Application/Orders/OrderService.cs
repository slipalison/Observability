using System.Diagnostics;
using Application.Orders;
using Microsoft.Extensions.Logging;
using Observability.WebApi.Domain.Orders;

namespace Observability.WebApi.Application.Orders;

/// <summary>
/// Orquestra as operações relacionadas a pedidos.
/// Implementa a lógica do caso de uso, utilizando a entidade de domínio
/// e as abstrações de infraestrutura (como o repositório).
/// </summary>
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IECommerceMetrics _metrics;

    public OrderService(
        ILogger<OrderService> logger, 
        IOrderRepository orderRepository, 
        IECommerceMetrics metrics)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _metrics = metrics;
    }

    /// <summary>
    /// Caso de uso para criar um novo pedido.
    /// </summary>
    public async Task<Order> CreateAsync(Guid userId, decimal totalAmount, CancellationToken cancellationToken)
    {
        var tags = new TagList();
        try
        {
            // 1. Usa o método de fábrica do domínio para criar a entidade
            var order = Order.Create(userId, totalAmount);

            // 2. Persiste a entidade usando a abstração do repositório
            await _orderRepository.SaveAsync(order, cancellationToken);
            
            // 3. Marca o pedido como concluído (lógica de domínio)
            order.MarkAsCompleted(); // Supondo que a persistência foi bem-sucedida

            _logger.LogInformation("Pedido {OrderId} criado com sucesso para o usuário {UserId}", order.Id, userId);
            
            // 4. Registra as MÉTRICAS DE NEGÓCIO de sucesso
            tags.Add("status", "completed");
            _metrics.OrdersCreatedCount.Add(1, tags);
            _metrics.OrdersValueTotal.Add((double)totalAmount, tags);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar o pedido para o usuário {UserId}", userId);
            
            // 5. Registra as MÉTRICAS DE NEGÓCIO de falha
            tags.Add("status", "failed");
            _metrics.OrdersCreatedCount.Add(1, tags);
            _metrics.OrdersValueTotal.Add((double)totalAmount, tags);

            // Propaga a exceção para a camada de apresentação tratar
            throw; 
        }
    }
}