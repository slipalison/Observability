using Microsoft.AspNetCore.Mvc;
using Observability.WebApi.Application.Orders;

namespace Observability.WebApi.Controllers;

/// <summary>
/// DTO (Data Transfer Object) para a requisição de criação de pedido.
/// É uma boa prática usar DTOs para desacoplar o contrato da API
/// das entidades de domínio internas.
/// </summary>
public record CreateOrderRequest(Guid UserId, decimal TotalAmount);


[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Cria um novo pedido.
    /// </summary>
    /// <param name="request">Dados para a criação do pedido.</param>
    /// <param name="cancellationToken">Token para cancelamento.</param>
    /// <returns>O pedido recém-criado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Observability.WebApi.Domain.Orders.Order), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrderAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(
            request.UserId,
            request.TotalAmount,
            cancellationToken);

        // A SOLUÇÃO: Usar CreatedAtRoute em vez de CreatedAtAction.
        // É uma abordagem mais robusta para gerar a URL de localização (Location).
        // A rota de destino (GetOrderById) foi nomeada como "GetOrderById".
        return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
    }
    
    /// <summary>
    /// Busca um pedido pelo ID.
    /// </summary>
    // A SOLUÇÃO: Adicionar um nome à rota usando o atributo 'Name'.
    // Isso permite que a rota seja referenciada de forma confiável.
    [HttpGet("{id:guid}", Name = "GetOrderById")]
    [ProducesResponseType(typeof(Observability.WebApi.Domain.Orders.Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetOrderByIdAsync(Guid id)
    {
        // Em uma implementação real, chamaríamos o service para buscar o pedido.
        // Aqui, apenas demonstramos o endpoint para o cabeçalho "Location".
        return Task.FromResult<IActionResult>(Ok(new { Message = $"Endpoint para buscar o pedido {id}." }));
    }
}