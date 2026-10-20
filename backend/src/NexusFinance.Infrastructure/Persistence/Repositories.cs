using Dapper;
using NexusFinance.Application.Abstractions;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connections;

    public UserRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM nexus.usuarios", cancellationToken: cancellationToken));
    }

    public async Task<UserRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            "SELECT id, nome, email, senha_hash, ativo FROM nexus.usuarios WHERE email = @email",
            new { email },
            cancellationToken: cancellationToken));
        return row is null ? null : new UserRecord(row.Id, row.Nome, row.Email, row.SenhaHash, row.Ativo);
    }

    public async Task AddAsync(string name, string email, string passwordHash, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO nexus.usuarios (nome, email, senha_hash, ativo) VALUES (@name, @email, @passwordHash, true)",
            new { name, email, passwordHash },
            cancellationToken: cancellationToken));
    }

    private sealed class UserRow
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string SenhaHash { get; set; } = "";
        public bool Ativo { get; set; }
    }
}

public sealed class FinanceSettingsRepository : IFinanceSettingsRepository
{
    private readonly IDbConnectionFactory _connections;

    public FinanceSettingsRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<FinanceSettings> GetAsync(CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = (await connection.QueryAsync<SettingRow>(new CommandDefinition(
            "SELECT chave, valor FROM nexus.configuracoes", cancellationToken: cancellationToken))).ToDictionary(row => row.Chave, row => row.Valor);
        return new FinanceSettings(rows.GetValueOrDefault("multa_percentual", 2), rows.GetValueOrDefault("juros_mensal_percentual", 1));
    }

    private sealed class SettingRow
    {
        public string Chave { get; set; } = "";
        public decimal Valor { get; set; }
    }
}

public sealed class ClientRepository : IClientRepository
{
    private readonly IDbConnectionFactory _connections;

    public ClientRepository(IDbConnectionFactory connections) => _connections = connections;

    public async Task<PagedClients> SearchAsync(int page, int pageSize, string? search, ClientStatus? status, CancellationToken cancellationToken)
    {
        var pattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var statusDb = status is null ? null : DbMaps.ToDb(status.Value);
        var sql = """
            SELECT id, nome, documento, email, telefone, status, criado_em
            FROM nexus.clientes
            WHERE (CAST(@pattern AS text) IS NULL OR nome ILIKE CAST(@pattern AS text) OR documento ILIKE CAST(@pattern AS text))
              AND (CAST(@status AS text) IS NULL OR status = CAST(@status AS text))
            ORDER BY nome
            LIMIT @limit OFFSET @offset
            """;
        var countSql = """
            SELECT COUNT(*)
            FROM nexus.clientes
            WHERE (CAST(@pattern AS text) IS NULL OR nome ILIKE CAST(@pattern AS text) OR documento ILIKE CAST(@pattern AS text))
              AND (CAST(@status AS text) IS NULL OR status = CAST(@status AS text))
            """;
        var parameters = new { pattern, status = statusDb, limit = pageSize, offset = (page - 1) * pageSize };
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ClientRow>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        return new PagedClients(rows.Select(Map).ToArray(), total);
    }

    public async Task<ClientRecord?> GetAsync(int id, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(new CommandDefinition(
            "SELECT id, nome, documento, email, telefone, status, criado_em FROM nexus.clientes WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<ClientRecord?> FindByDocumentAsync(string document, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ClientRow>(new CommandDefinition(
            "SELECT id, nome, documento, email, telefone, status, criado_em FROM nexus.clientes WHERE documento = @document",
            new { document },
            cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<ClientRecord>> ListOptionsAsync(CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        var rows = await connection.QueryAsync<ClientRow>(new CommandDefinition(
            "SELECT id, nome, documento, email, telefone, status, criado_em FROM nexus.clientes ORDER BY nome",
            cancellationToken: cancellationToken));
        return rows.Select(Map).ToArray();
    }

    public async Task<int> AddAsync(ClientRecord client, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            INSERT INTO nexus.clientes (nome, documento, email, telefone, status)
            VALUES (@Name, @Document, @Email, @Phone, @Status)
            RETURNING id
            """,
            new { client.Name, client.Document, client.Email, client.Phone, Status = DbMaps.ToDb(client.Status) },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(ClientRecord client, CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE nexus.clientes
            SET nome = @Name, documento = @Document, email = @Email, telefone = @Phone, status = @Status
            WHERE id = @Id
            """,
            new { client.Id, client.Name, client.Document, client.Email, client.Phone, Status = DbMaps.ToDb(client.Status) },
            cancellationToken: cancellationToken));
    }

    private static ClientRecord Map(ClientRow row) =>
        new(row.Id, row.Nome, row.Documento, row.Email, row.Telefone, DbMaps.ToClientStatus(row.Status), row.CriadoEm);

    private sealed class ClientRow
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string Documento { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Telefone { get; set; }
        public string Status { get; set; } = "";
        public DateTime CriadoEm { get; set; }
    }
}
