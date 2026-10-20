using System.Reflection;
using Dapper;
using NexusFinance.Application.Abstractions;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class MigrationRunner
{
    private readonly IDbConnectionFactory _connections;

    public MigrationRunner(IDbConnectionFactory connections)
    {
        _connections = connections;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        using var connection = _connections.CreateConnection();
        connection.Open();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            CREATE SCHEMA IF NOT EXISTS nexus;
            CREATE TABLE IF NOT EXISTS nexus.schema_migrations (
                id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                nome varchar(200) NOT NULL UNIQUE,
                aplicada_em timestamptz NOT NULL DEFAULT now()
            );
            """,
            cancellationToken: cancellationToken));
        var applied = (await connection.QueryAsync<string>(
            new CommandDefinition("SELECT nome FROM nexus.schema_migrations", cancellationToken: cancellationToken))).ToHashSet();

        var scripts = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(name => name.StartsWith("migrations.", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var resourceName in scripts)
        {
            var fileName = resourceName["migrations.".Length..];
            if (applied.Contains(fileName))
            {
                continue;
            }

            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO nexus.schema_migrations (nome) VALUES (@nome) ON CONFLICT (nome) DO NOTHING",
                new { nome = fileName },
                cancellationToken: cancellationToken));
        }
    }
}
