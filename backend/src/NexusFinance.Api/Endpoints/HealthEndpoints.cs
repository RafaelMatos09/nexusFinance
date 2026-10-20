using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Health;

namespace NexusFinance.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", Check).WithName("Health").WithTags("Health");
        app.MapGet("/api/health", Check).WithName("ApiHealth").WithTags("Health");
        return app;
    }

    private static async Task<IResult> Check(IHealthProbe probe, CancellationToken cancellationToken)
    {
        var databaseOk = await probe.DatabaseIsReachableAsync(cancellationToken);
        var status = new HealthStatusDto(
            databaseOk ? "ok" : "degraded",
            databaseOk ? "ok" : "unavailable");

        return databaseOk
            ? Results.Ok(status)
            : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
