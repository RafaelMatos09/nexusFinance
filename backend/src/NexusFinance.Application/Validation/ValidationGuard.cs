using FluentValidation;
using NexusFinance.Application.Common;

namespace NexusFinance.Application.Validation;

internal static class ValidationGuard
{
    public static async Task EnsureValid<T>(this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        throw new AppValidationException(errors);
    }
}
