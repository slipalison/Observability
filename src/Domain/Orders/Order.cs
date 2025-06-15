namespace Observability.WebApi.Domain.Orders;

/// <summary>
/// Representa a entidade Agregada de um Pedido.
/// Contém as regras de negócio e o estado do pedido.
/// Esta classe é "limpa", não tem dependências de frameworks ou infraestrutura.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor privado para forçar a criação via método de fábrica,
    // garantindo que a entidade seja sempre criada em um estado válido.
    private Order(Guid userId, decimal totalAmount)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TotalAmount = totalAmount;
        Status = "Pending"; // Status inicial padrão
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Método de Fábrica (Factory Method) para criar uma nova instância de Pedido.
    /// Centraliza a lógica de criação e validação inicial.
    /// </summary>
    public static Order Create(Guid userId, decimal totalAmount)
    {
        // Regra de negócio: um pedido não pode ter valor negativo ou zero.
        if (totalAmount <= 0)
        {
            throw new ArgumentException("O valor total do pedido deve ser positivo.", nameof(totalAmount));
        }

        return new Order(userId, totalAmount);
    }

    public void MarkAsCompleted()
    {
        // Regra de negócio: só pode completar um pedido que está pendente.
        if (Status != "Pending")
        {
            throw new InvalidOperationException("Apenas pedidos pendentes podem ser marcados como concluídos.");
        }
        Status = "Completed";
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
    }
}