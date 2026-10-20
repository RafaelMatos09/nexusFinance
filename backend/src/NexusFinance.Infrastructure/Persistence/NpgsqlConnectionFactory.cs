using System.Data;
using Microsoft.Extensions.Configuration;
using NexusFinance.Application.Abstractions;
using Npgsql;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'Default' não está configurada. Defina-a com user-secrets.");
        }
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
