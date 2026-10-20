using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using NexusFinance.Application.Abstractions;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class DapperHealthProbe : IHealthProbe
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DapperHealthProbe> _logger;

    public DapperHealthProbe(IDbConnectionFactory connectionFactory, ILogger<DapperHealthProbe> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<bool> DatabaseIsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }

            var result = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));

            return result == 1;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Não foi possível consultar o PostgreSQL.");
            return false;
        }
    }
}
