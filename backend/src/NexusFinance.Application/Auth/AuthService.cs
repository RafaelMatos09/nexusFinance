using FluentValidation;
using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Common;
using NexusFinance.Application.Validation;

namespace NexusFinance.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string Token, string Name, string Email);

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;
    private readonly ITokenService _tokens;
    private readonly IValidator<LoginRequest> _validator;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwords,
        ITokenService tokens,
        IValidator<LoginRequest> validator)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _validator = validator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.Active || !_passwords.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return new LoginResponse(_tokens.Create(user.Id, user.Name, user.Email), user.Name, user.Email);
    }
}
