using System.Diagnostics;
using Application.Orders;
using Dapper;
using Npgsql;
using Observability.WebApi.Domain.Orders;
using Observability.WebApi.Infrastructure.Metrics;

namespace Observability.WebApi.Infrastructure.Persistence;

/// <summary>
/// Implementação do repositório de pedidos para PostgreSQL usando Dapper.
/// Esta classe é responsável por toda a interação com o banco de dados.
/// </summary>
public class PostgresOrderRepository : IOrderRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ECommerceMetrics _metrics;

    public PostgresOrderRepository(NpgsqlDataSource dataSource, ECommerceMetrics metrics)
    {
        _dataSource = dataSource;
        _metrics = metrics;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """SELECT * FROM public.orders WHERE "Id" = @Id""";
        return await ExecuteWithInstrumentationAsync(
            "SELECT",
            async () =>
            {
                // SOLUÇÃO: A conexão é obtida e automaticamente descartada no final do bloco 'using'.
                await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
                return await connection.QuerySingleOrDefaultAsync<Order>(sql, new { Id = id });
            }
        );
    }

    public async Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.orders ("Id", "UserId", "TotalAmount", "Status", "CreatedAt") 
            VALUES (@Id, @UserId, @TotalAmount, @Status, @CreatedAt)
            """;
        
        // O tipo de retorno de ExecuteAsync é Task<int>, então T em ExecuteWithInstrumentationAsync será int.
        await ExecuteWithInstrumentationAsync<int>(
            "INSERT",
            async () =>
            {
                // SOLUÇÃO: Garante que a conexão seja devolvida ao pool.
                await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
                return await connection.ExecuteAsync(sql, order);
            }
        );
    }

    /// <summary>
    /// Wrapper para executar e instrumentar operações de banco de dados.
    /// </summary>
    private async Task<T> ExecuteWithInstrumentationAsync<T>(string operationName, Func<Task<T>> databaseFunc)
    {
        var tags = new TagList { { "db.operation.name", operationName } };
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            return await databaseFunc();
        }
        catch
        {
            tags.Add("db.operation.status", "failed");
            throw; // Re-lança a exceção para ser tratada pela camada de aplicação/apresentação
        }
        finally
        {
            stopwatch.Stop();
            _metrics.DatabaseOperationDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }
    }
}