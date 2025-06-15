

using Observability.WebApi.Domain.Orders;

namespace Application.Orders;

/// <summary>
/// Define o contrato para as operações de persistência da entidade Order.
/// Esta abstração permite que a camada de aplicação seja agnóstica
/// em relação à tecnologia de banco de dados utilizada.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Salva um novo pedido no repositório.
    /// </summary>
    /// <param name="order">A entidade de pedido a ser salva.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>A Task da operação assíncrona.</returns>
    Task SaveAsync(Order order, CancellationToken cancellationToken);

    /// <summary>
    /// Busca um pedido pelo seu identificador único.
    /// </summary>
    /// <param name="id">O ID do pedido.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>A entidade de pedido, ou null se não for encontrada.</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}


