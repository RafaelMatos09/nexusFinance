using FluentValidation;
using NexusFinance.Application.Auth;
using NexusFinance.Application.Charges;
using NexusFinance.Application.Clients;
using NexusFinance.Application.Contracts;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(180);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(100);
    }
}

public sealed class SaveClientRequestValidator : AbstractValidator<SaveClientRequest>
{
    public SaveClientRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Document)
            .NotEmpty()
            .Must(DocumentoRules.IsValid)
            .WithMessage("Informe um CPF ou CNPJ válido.");
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(180);
        RuleFor(request => request.Phone).MaximumLength(20);
    }
}

public sealed class SaveContractRequestValidator : AbstractValidator<SaveContractRequest>
{
    public SaveContractRequestValidator()
    {
        RuleFor(request => request.ClientId).GreaterThan(0);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Amount).GreaterThan(0);
        RuleFor(request => request.Frequency).IsInEnum();
        RuleFor(request => request.EndDate)
            .Must((request, endDate) => endDate is null || endDate >= request.StartDate)
            .WithMessage("A data final não pode ser anterior à data inicial.");
    }
}

public sealed class RegisterPaymentRequestValidator : AbstractValidator<RegisterPaymentRequest>
{
    public RegisterPaymentRequestValidator()
    {
        RuleFor(request => request.Amount).GreaterThan(0);
        RuleFor(request => request.Method).IsInEnum();
    }
}
