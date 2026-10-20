using FluentValidation;
using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Common;
using NexusFinance.Application.Validation;
using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Application.Contracts;

public sealed record SaveContractRequest(
    int ClientId,
    string Description,
    decimal Amount,
    BillingFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed record ChangeContractStatusRequest(ContractStatus Status);

public sealed record ContractDto(
    int Id,
    int ClientId,
    string ClientName,
    string Description,
    decimal Amount,
    BillingFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    ContractStatus Status);

public sealed class ContractService
{
    private readonly IContractRepository _contracts;
    private readonly IClientRepository _clients;
    private readonly IChargeRepository _charges;
    private readonly IClock _clock;
    private readonly IValidator<SaveContractRequest> _validator;

    public ContractService(
        IContractRepository contracts,
        IClientRepository clients,
        IChargeRepository charges,
        IClock clock,
        IValidator<SaveContractRequest> validator)
    {
        _contracts = contracts;
        _clients = clients;
        _charges = charges;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<ContractDto>> SearchAsync(
        int page,
        int pageSize,
        int? clientId,
        ContractStatus? status,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 10 : pageSize;
        var result = await _contracts.SearchAsync(page, pageSize, clientId, status, cancellationToken);
        return new PagedResult<ContractDto>(result.Items.Select(Map).ToArray(), page, pageSize, result.Total);
    }

    public async Task<IReadOnlyList<ContractDto>> ListByClientAsync(int clientId, CancellationToken cancellationToken)
    {
        _ = await _clients.GetAsync(clientId, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        var contracts = await _contracts.ListByClientAsync(clientId, cancellationToken);
        return contracts.Select(Map).ToArray();
    }

    public async Task<ContractDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var contract = await _contracts.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Contrato não encontrado.");
        return Map(contract);
    }

    public async Task<ContractDto> CreateAsync(SaveContractRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        var client = await _clients.GetAsync(request.ClientId, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        if (client.Status == ClientStatus.Inactive)
        {
            throw new Domain.Exceptions.DomainException("Cliente inativo não pode receber contrato.");
        }

        var id = await _contracts.AddAsync(
            new ContractRecord(0, client.Id, client.Name, request.Description.Trim(), request.Amount, request.Frequency, request.StartDate, request.EndDate, ContractStatus.Draft),
            cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<ContractDto> UpdateAsync(int id, SaveContractRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        var current = await _contracts.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Contrato não encontrado.");
        if (current.Status != ContractStatus.Draft)
        {
            throw new Domain.Exceptions.DomainException("Somente contratos em rascunho podem ser editados.");
        }

        var client = await _clients.GetAsync(request.ClientId, cancellationToken) ?? throw new NotFoundException("Cliente não encontrado.");
        await _contracts.UpdateAsync(
            current with
            {
                ClientId = client.Id,
                ClientName = client.Name,
                Description = request.Description.Trim(),
                Amount = request.Amount,
                Frequency = request.Frequency,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            },
            cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<ContractDto> ChangeStatusAsync(int id, ContractStatus status, CancellationToken cancellationToken)
    {
        var current = await _contracts.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Contrato não encontrado.");
        ContractStatusRules.EnsureTransition(current.Status, status);
        await _contracts.UpdateAsync(current with { Status = status }, cancellationToken);

        if (status == ContractStatus.Active)
        {
            await GenerateChargesAsync(id, cancellationToken);
        }

        if (status == ContractStatus.Cancelled)
        {
            await _charges.CancelOpenWithoutPaymentsAsync(id, cancellationToken);
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<int> GenerateChargesAsync(int id, CancellationToken cancellationToken)
    {
        var contract = await _contracts.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Contrato não encontrado.");
        if (contract.Status != ContractStatus.Active)
        {
            throw new Domain.Exceptions.DomainException("Somente contrato ativo gera cobranças.");
        }

        var existing = await _charges.ListDueDatesAsync(id, cancellationToken);
        var dueDates = ChargeScheduleRules.ListDueDates(contract.StartDate, contract.EndDate, contract.Frequency, _clock.Today);
        var created = 0;
        foreach (var dueDate in dueDates.Except(existing))
        {
            await _charges.AddAsync(contract.Id, contract.ClientId, contract.Amount, dueDate, ChargeStatus.Pending, cancellationToken);
            created++;
        }

        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        return created;
    }

    private static ContractDto Map(ContractRecord contract) =>
        new(contract.Id, contract.ClientId, contract.ClientName, contract.Description, contract.Amount, contract.Frequency, contract.StartDate, contract.EndDate, contract.Status);
}
