using Dapper;
using NexusFinance.Application.Abstractions;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class ContractRepository : IContractRepository
{
    private readonly IDbConnectionFactory _connections;

    public ContractRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<PagedContracts> SearchAsync(int page, int pageSize, int? clientId, ContractStatus? status, CancellationToken cancellationToken)
    {
        const string filter = """
            WHERE (CAST(@clientId AS integer) IS NULL OR c.cliente_id = CAST(@clientId AS integer))
              AND (CAST(@status AS text) IS NULL OR c.status = CAST(@status AS text))
            """;
        var parameters = new
        {
            clientId,
            status = status is null ? null : DbMaps.ToDb(status.Value),
            limit = pageSize,
            offset = (page - 1) * pageSize
        };
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ContractRow>(new CommandDefinition(
            $"""
            SELECT c.id, c.cliente_id, cl.nome AS cliente_nome, c.descricao, c.valor, c.periodicidade, c.data_inicio, c.data_fim, c.status
            FROM nexus.contratos c
            JOIN nexus.clientes cl ON cl.id = c.cliente_id
            {filter}
            ORDER BY c.id DESC
            LIMIT @limit OFFSET @offset
            """,
            parameters,
            cancellationToken: cancellationToken));
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM nexus.contratos c {filter}",
            parameters,
            cancellationToken: cancellationToken));
        return new PagedContracts(rows.Select(Map).ToArray(), total);
    }

    public async Task<ContractRecord?> GetAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ContractRow>(new CommandDefinition(
            """
            SELECT c.id, c.cliente_id, cl.nome AS cliente_nome, c.descricao, c.valor, c.periodicidade, c.data_inicio, c.data_fim, c.status
            FROM nexus.contratos c
            JOIN nexus.clientes cl ON cl.id = c.cliente_id
            WHERE c.id = @id
            """,
            new { id },
            cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<ContractRecord>> ListByClientAsync(int clientId, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ContractRow>(new CommandDefinition(
            """
            SELECT c.id, c.cliente_id, cl.nome AS cliente_nome, c.descricao, c.valor, c.periodicidade, c.data_inicio, c.data_fim, c.status
            FROM nexus.contratos c
            JOIN nexus.clientes cl ON cl.id = c.cliente_id
            WHERE c.cliente_id = @clientId
            ORDER BY c.id DESC
            """,
            new { clientId },
            cancellationToken: cancellationToken));
        return rows.Select(Map).ToArray();
    }

    public async Task<int> AddAsync(ContractRecord contract, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO nexus.contratos (cliente_id, descricao, valor, periodicidade, data_inicio, data_fim, status)
            VALUES (@ClientId, @Description, @Amount, @Frequency, @StartDate, @EndDate, @Status)
            RETURNING id
            """,
            new
            {
                contract.ClientId,
                contract.Description,
                contract.Amount,
                Frequency = DbMaps.ToDb(contract.Frequency),
                contract.StartDate,
                contract.EndDate,
                Status = DbMaps.ToDb(contract.Status)
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(ContractRecord contract, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE nexus.contratos
            SET cliente_id = @ClientId, descricao = @Description, valor = @Amount, periodicidade = @Frequency,
                data_inicio = @StartDate, data_fim = @EndDate, status = @Status
            WHERE id = @Id
            """,
            new
            {
                contract.Id,
                contract.ClientId,
                contract.Description,
                contract.Amount,
                Frequency = DbMaps.ToDb(contract.Frequency),
                contract.StartDate,
                contract.EndDate,
                Status = DbMaps.ToDb(contract.Status)
            },
            cancellationToken: cancellationToken));
    }

    private static ContractRecord Map(ContractRow row) =>
        new(row.Id, row.ClienteId, row.ClienteNome, row.Descricao, row.Valor, DbMaps.ToFrequency(row.Periodicidade), row.DataInicio, row.DataFim, DbMaps.ToContractStatus(row.Status));

    private sealed class ContractRow
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
        public string Periodicidade { get; set; } = "";
        public DateOnly DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }
        public string Status { get; set; } = "";
    }
}
