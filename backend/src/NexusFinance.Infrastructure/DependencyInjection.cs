using Microsoft.Extensions.DependencyInjection;
using NexusFinance.Application.Abstractions;
using NexusFinance.Infrastructure.Persistence;

namespace NexusFinance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IHealthProbe, DapperHealthProbe>();
        return services;
    }
}
