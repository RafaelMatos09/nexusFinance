using Dapper;
using Microsoft.Extensions.DependencyInjection;
using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Reports;
using NexusFinance.Infrastructure.Persistence;
using NexusFinance.Infrastructure.Reporting;
using NexusFinance.Infrastructure.Security;
using NexusFinance.Infrastructure.Time;

namespace NexusFinance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddSingleton<IHealthProbe, DapperHealthProbe>();
        services.AddSingleton<IClock, BrazilClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IReportWorkbookExporter, ClosedXmlReportExporter>();
        services.AddScoped<MigrationRunner>();
        services.AddScoped<DemoSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFinanceSettingsRepository, FinanceSettingsRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IChargeRepository, ChargeRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        return services;
    }
}
