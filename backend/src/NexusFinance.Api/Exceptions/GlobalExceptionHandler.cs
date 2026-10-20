using Microsoft.AspNetCore.Diagnostics;

namespace NexusFinance.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Erro não tratado em {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var exposeDetail = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");

        await Results.Problem(
            title: "Erro interno do servidor",
            detail: exposeDetail ? exception.Message : "Ocorreu um erro inesperado.",
            statusCode: StatusCodes.Status500InternalServerError,
            instance: httpContext.Request.Path
        ).ExecuteAsync(httpContext);

        return true;
    }
}
