using Dapper;
using NexusFinance.Application.Abstractions;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class ChargeRepository : IChargeRepository
{
    private const string SelectSql = """
        SELECT c.id, c.contrato_id, c.cliente_id, cl.nome AS cliente_nome, ct.descricao AS contrato_descricao,
               c.valor, c.vencimento, c.status, c.observacao,
               COALESCE((SELECT SUM(r.valor_pago) FROM nexus.recebimentos r WHERE r.cobranca_id = c.id), 0) AS total_pago
        FROM nexus.cobrancas c
        JOIN nexus.clientes cl ON cl.id = c.cliente_id
        JOIN nexus.contratos ct ON ct.id = c.contrato_id
        """;

    private readonly IDbConnectionFactory _connections;

    public ChargeRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task MarkOverdueAsync(DateOnly today, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE nexus.cobrancas SET status = 'Vencida' WHERE status = 'Pendente' AND vencimento < @today",
            new { today },
            cancellationToken: cancellationToken));
    }

    public async Task<PagedCharges> SearchAsync(ChargeFilter filter, CancellationToken cancellationToken)
    {
        var parameters = Parameters(filter);
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ChargeRow>(new CommandDefinition(
            $"""
            {SelectSql}
            {Where}
            ORDER BY c.vencimento, c.id
            LIMIT @limit OFFSET @offset
            """,
            parameters,
            cancellationToken: cancellationToken));
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM nexus.cobrancas c {Where}",
            parameters,
            cancellationToken: cancellationToken));
        return new PagedCharges(rows.Select(Map).ToArray(), total);
    }

    public async Task<IReadOnlyList<ChargeRecord>> ListAllAsync(ChargeFilter filter, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ChargeRow>(new CommandDefinition(
            $"""
            {SelectSql}
            {Where}
            ORDER BY c.vencimento, c.id
            """,
            Parameters(filter),
            cancellationToken: cancellationToken));
        return rows.Select(Map).ToArray();
    }

    public async Task<ChargeRecord?> GetAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ChargeRow>(new CommandDefinition(
            $"{SelectSql} WHERE c.id = @id",
            new { id },
            cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<DateOnly>> ListDueDatesAsync(int contractId, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var dates = await connection.QueryAsync<DateOnly>(new CommandDefinition(
            "SELECT vencimento FROM nexus.cobrancas WHERE contrato_id = @contractId",
            new { contractId },
            cancellationToken: cancellationToken));
        return dates.ToArray();
    }

    public async Task AddAsync(int contractId, int clientId, decimal amount, DateOnly dueDate, ChargeStatus status, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO nexus.cobrancas (contrato_id, cliente_id, valor, vencimento, status)
            VALUES (@contractId, @clientId, @amount, @dueDate, @status)
            """,
            new { contractId, clientId, amount, dueDate, status = DbMaps.ToDb(status) },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateStatusAsync(int id, ChargeStatus status, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE nexus.cobrancas SET status = @status WHERE id = @id",
            new { id, status = DbMaps.ToDb(status) },
            cancellationToken: cancellationToken));
    }

    public async Task CancelOpenWithoutPaymentsAsync(int contractId, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE nexus.cobrancas c
            SET status = 'Cancelada'
            WHERE c.contrato_id = @contractId
              AND c.status IN ('Pendente', 'Vencida')
              AND NOT EXISTS (SELECT 1 FROM nexus.recebimentos r WHERE r.cobranca_id = c.id)
            """,
            new { contractId },
            cancellationToken: cancellationToken));
    }

    private const string Where = """
        WHERE (CAST(@clientId AS integer) IS NULL OR c.cliente_id = CAST(@clientId AS integer))
          AND (CAST(@status AS text) IS NULL OR c.status = CAST(@status AS text))
          AND (CAST(@fromDate AS date) IS NULL OR c.vencimento >= CAST(@fromDate AS date))
          AND (CAST(@toDate AS date) IS NULL OR c.vencimento <= CAST(@toDate AS date))
        """;

    private static object Parameters(ChargeFilter filter) => new
    {
        clientId = filter.ClientId,
        status = filter.Status is null ? null : DbMaps.ToDb(filter.Status.Value),
        fromDate = filter.From,
        toDate = filter.To,
        limit = filter.PageSize,
        offset = (filter.Page - 1) * filter.PageSize
    };

    private static ChargeRecord Map(ChargeRow row) =>
        new(row.Id, row.ContratoId, row.ClienteId, row.ClienteNome, row.ContratoDescricao, row.Valor, row.Vencimento, DbMaps.ToChargeStatus(row.Status), row.Observacao, row.TotalPago);

    private sealed class ChargeRow
    {
        public int Id { get; set; }
        public int ContratoId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = "";
        public string ContratoDescricao { get; set; } = "";
        public decimal Valor { get; set; }
        public DateOnly Vencimento { get; set; }
        public string Status { get; set; } = "";
        public string? Observacao { get; set; }
        public decimal TotalPago { get; set; }
    }
}

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connections;

    public PaymentRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<int> AddAsync(int chargeId, decimal amount, DateOnly paidOn, PaymentMethod method, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO nexus.recebimentos (cobranca_id, valor_pago, data_pagamento, forma_pagamento)
            VALUES (@chargeId, @amount, @paidOn, @method)
            RETURNING id
            """,
            new { chargeId, amount, paidOn, method = DbMaps.ToDb(method) },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PaymentRecord>> ListByChargeAsync(int chargeId, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<PaymentRow>(new CommandDefinition(
            """
            SELECT id, cobranca_id, valor_pago, data_pagamento, forma_pagamento
            FROM nexus.recebimentos
            WHERE cobranca_id = @chargeId
            ORDER BY data_pagamento, id
            """,
            new { chargeId },
            cancellationToken: cancellationToken));
        return rows.Select(row => new PaymentRecord(row.Id, row.CobrancaId, row.ValorPago, row.DataPagamento, DbMaps.ToPaymentMethod(row.FormaPagamento))).ToArray();
    }

    public async Task<IReadOnlyList<MonthlyTotal>> SumByMonthAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<MonthRow>(new CommandDefinition(
            """
            SELECT EXTRACT(YEAR FROM data_pagamento)::int AS year,
                   EXTRACT(MONTH FROM data_pagamento)::int AS month,
                   SUM(valor_pago) AS amount
            FROM nexus.recebimentos
            WHERE data_pagamento >= @from AND data_pagamento <= @to
            GROUP BY 1, 2
            """,
            new { from, to },
            cancellationToken: cancellationToken));
        return rows.Select(row => new MonthlyTotal(row.Year, row.Month, row.Amount)).ToArray();
    }

    private sealed class PaymentRow
    {
        public int Id { get; set; }
        public int CobrancaId { get; set; }
        public decimal ValorPago { get; set; }
        public DateOnly DataPagamento { get; set; }
        public string FormaPagamento { get; set; } = "";
    }

    private sealed class MonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }
}
