using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NexusFinance.Application.Auth;
using NexusFinance.Application.Charges;
using NexusFinance.Application.Clients;
using NexusFinance.Application.Contracts;
using NexusFinance.Application.Dashboard;
using NexusFinance.Application.Reports;

namespace NexusFinance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ClientService>();
        services.AddScoped<ContractService>();
        services.AddScoped<ChargeService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReportService>();
        return services;
    }
}
