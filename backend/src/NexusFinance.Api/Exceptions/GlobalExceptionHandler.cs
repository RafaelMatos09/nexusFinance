using Microsoft.AspNetCore.Diagnostics;
using NexusFinance.Application.Common;
using NexusFinance.Domain.Exceptions;

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
        var (status, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Credenciais inválidas"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erro não tratado em {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var exposeDetail = status != StatusCodes.Status500InternalServerError
            || _environment.IsDevelopment()
            || _environment.IsEnvironment("Testing");

        IDictionary<string, object?>? extensions = exception is AppValidationException validation
            ? new Dictionary<string, object?> { ["errors"] = validation.Errors }
            : null;

        await Results.Problem(
            title: title,
            detail: exposeDetail ? exception.Message : "Ocorreu um erro inesperado.",
            statusCode: status,
            instance: httpContext.Request.Path,
            extensions: extensions
        ).ExecuteAsync(httpContext);

        return true;
    }
}
