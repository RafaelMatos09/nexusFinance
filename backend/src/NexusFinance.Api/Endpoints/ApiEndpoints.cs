using NexusFinance.Application.Auth;
using NexusFinance.Application.Charges;
using NexusFinance.Application.Clients;
using NexusFinance.Application.Contracts;
using NexusFinance.Application.Dashboard;
using NexusFinance.Application.Reports;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth, CancellationToken cancellationToken) =>
            Results.Ok(await auth.LoginAsync(request, cancellationToken)));

        var clients = app.MapGroup("/api/clients").RequireAuthorization();
        clients.MapGet("/", async (int? page, int? pageSize, string? search, ClientStatus? status, ClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SearchAsync(page ?? 1, pageSize ?? 10, search, status, cancellationToken)));
        clients.MapGet("/options", async (ClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListOptionsAsync(cancellationToken)));
        clients.MapGet("/{id:int}", async (int id, ClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(id, cancellationToken)));
        clients.MapGet("/{id:int}/contracts", async (int id, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListByClientAsync(id, cancellationToken)));
        clients.MapPost("/", async (SaveClientRequest request, ClientService service, CancellationToken cancellationToken) =>
            Results.Created("/api/clients", await service.CreateAsync(request, cancellationToken)));
        clients.MapPut("/{id:int}", async (int id, SaveClientRequest request, ClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(id, request, cancellationToken)));
        clients.MapPost("/{id:int}/inactivate", async (int id, ClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.InactivateAsync(id, cancellationToken)));

        var contracts = app.MapGroup("/api/contracts").RequireAuthorization();
        contracts.MapGet("/", async (int? page, int? pageSize, int? clientId, ContractStatus? status, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SearchAsync(page ?? 1, pageSize ?? 10, clientId, status, cancellationToken)));
        contracts.MapGet("/{id:int}", async (int id, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(id, cancellationToken)));
        contracts.MapPost("/", async (SaveContractRequest request, ContractService service, CancellationToken cancellationToken) =>
            Results.Created("/api/contracts", await service.CreateAsync(request, cancellationToken)));
        contracts.MapPut("/{id:int}", async (int id, SaveContractRequest request, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(id, request, cancellationToken)));
        contracts.MapPost("/{id:int}/status", async (int id, ChangeContractStatusRequest request, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ChangeStatusAsync(id, request.Status, cancellationToken)));
        contracts.MapPost("/{id:int}/charges", async (int id, ContractService service, CancellationToken cancellationToken) =>
            Results.Ok(new { created = await service.GenerateChargesAsync(id, cancellationToken) }));

        var charges = app.MapGroup("/api/charges").RequireAuthorization();
        charges.MapGet("/", async (int? page, int? pageSize, int? clientId, ChargeStatus? status, DateOnly? from, DateOnly? to, ChargeService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SearchAsync(page ?? 1, pageSize ?? 10, clientId, status, from, to, cancellationToken)));
        charges.MapGet("/{id:int}", async (int id, ChargeService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(id, cancellationToken)));
        charges.MapGet("/{id:int}/payments", async (int id, ChargeService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListPaymentsAsync(id, cancellationToken)));
        charges.MapPost("/{id:int}/payments", async (int id, RegisterPaymentRequest request, ChargeService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.RegisterPaymentAsync(id, request, cancellationToken)));
        charges.MapPost("/{id:int}/cancel", async (int id, ChargeService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CancelAsync(id, cancellationToken)));

        app.MapGet("/api/dashboard", async (DashboardService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(cancellationToken))).RequireAuthorization();

        var reports = app.MapGroup("/api/reports").RequireAuthorization();
        reports.MapGet("/charges", async (int? clientId, ChargeStatus? status, DateOnly? from, DateOnly? to, ReportService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(clientId, status, from, to, cancellationToken)));
        reports.MapGet("/charges/excel", async (int? clientId, ChargeStatus? status, DateOnly? from, DateOnly? to, ReportService service, IReportWorkbookExporter exporter, CancellationToken cancellationToken) =>
        {
            var report = await service.GetAsync(clientId, status, from, to, cancellationToken);
            var bytes = exporter.Export(report);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cobrancas.xlsx");
        });

        return app;
    }
}
