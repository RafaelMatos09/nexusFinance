using FluentValidation;
using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Common;
using NexusFinance.Application.Validation;
using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Application.Clients;

public sealed record SaveClientRequest(string Name, string Document, string Email, string? Phone);

public sealed record ClientDto(
    int Id,
    string Name,
    string Document,
    string Email,
    string? Phone,
    ClientStatus Status,
    DateTime CreatedAt);

public sealed class ClientService
{
    private readonly IClientRepository _clients;
    private readonly IValidator<SaveClientRequest> _validator;

    public ClientService(IClientRepository clients, IValidator<SaveClientRequest> validator)
    {
        _clients = clients;
        _validator = validator;
    }

    public async Task<PagedResult<ClientDto>> SearchAsync(
        int page,
        int pageSize,
        string? search,
        ClientStatus? status,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 10 : pageSize;
        var result = await _clients.SearchAsync(page, pageSize, search, status, cancellationToken);
        return new PagedResult<ClientDto>(result.Items.Select(Map).ToArray(), page, pageSize, result.Total);
    }

    public async Task<ClientDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var client = await _clients.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        return Map(client);
    }

    public async Task<IReadOnlyList<ClientDto>> ListOptionsAsync(CancellationToken cancellationToken)
    {
        var clients = await _clients.ListOptionsAsync(cancellationToken);
        return clients.Select(Map).ToArray();
    }

    public async Task<ClientDto> CreateAsync(SaveClientRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        var document = DocumentoRules.OnlyDigits(request.Document);
        var existing = await _clients.FindByDocumentAsync(document, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Já existe um cliente com este documento.");
        }

        var id = await _clients.AddAsync(
            new ClientRecord(0, request.Name.Trim(), document, request.Email.Trim().ToLowerInvariant(), NormalizePhone(request.Phone), ClientStatus.Active, DateTime.UtcNow),
            cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<ClientDto> UpdateAsync(int id, SaveClientRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        var current = await _clients.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        var document = DocumentoRules.OnlyDigits(request.Document);
        var existing = await _clients.FindByDocumentAsync(document, cancellationToken);
        if (existing is not null && existing.Id != id)
        {
            throw new ConflictException("Já existe um cliente com este documento.");
        }

        await _clients.UpdateAsync(
            current with
            {
                Name = request.Name.Trim(),
                Document = document,
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = NormalizePhone(request.Phone)
            },
            cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<ClientDto> InactivateAsync(int id, CancellationToken cancellationToken)
    {
        var current = await _clients.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        if (current.Status == ClientStatus.Inactive)
        {
            return Map(current);
        }

        await _clients.UpdateAsync(current with { Status = ClientStatus.Inactive }, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private static ClientDto Map(ClientRecord client) =>
        new(client.Id, client.Name, client.Document, client.Email, client.Phone, client.Status, client.CreatedAt);

    private static string? NormalizePhone(string? phone)
    {
        var trimmed = phone?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
