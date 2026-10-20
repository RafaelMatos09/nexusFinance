using FluentValidation;
using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Common;
using NexusFinance.Application.Validation;
using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Application.Charges;

public sealed record RegisterPaymentRequest(decimal Amount, DateOnly PaidOn, PaymentMethod Method);

public sealed record ChargeDto(
    int Id,
    int ContractId,
    int ClientId,
    string ClientName,
    string ContractDescription,
    decimal OriginalAmount,
    DateOnly DueDate,
    ChargeStatus Status,
    string? Notes,
    int DaysLate,
    decimal Fine,
    decimal Interest,
    decimal UpdatedAmount,
    decimal AmountPaid,
    decimal Balance);

public sealed record PaymentDto(int Id, int ChargeId, decimal Amount, DateOnly PaidOn, PaymentMethod Method);

public sealed class ChargeService
{
    private readonly IChargeRepository _charges;
    private readonly IPaymentRepository _payments;
    private readonly IFinanceSettingsRepository _settings;
    private readonly IClock _clock;
    private readonly IValidator<RegisterPaymentRequest> _validator;

    public ChargeService(
        IChargeRepository charges,
        IPaymentRepository payments,
        IFinanceSettingsRepository settings,
        IClock clock,
        IValidator<RegisterPaymentRequest> validator)
    {
        _charges = charges;
        _payments = payments;
        _settings = settings;
        _clock = clock;
        _validator = validator;
    }

    public async Task<PagedResult<ChargeDto>> SearchAsync(
        int page,
        int pageSize,
        int? clientId,
        ChargeStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 50 ? 10 : pageSize;
        var finance = await _settings.GetAsync(cancellationToken);
        var result = await _charges.SearchAsync(new ChargeFilter(page, pageSize, clientId, status, from, to), cancellationToken);
        return new PagedResult<ChargeDto>(result.Items.Select(item => Map(item, finance)).ToArray(), page, pageSize, result.Total);
    }

    public async Task<ChargeDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        var charge = await _charges.GetAsync(id, cancellationToken) ?? throw new NotFoundException("Cobrança não encontrada.");
        var finance = await _settings.GetAsync(cancellationToken);
        return Map(charge, finance);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(int chargeId, CancellationToken cancellationToken)
    {
        _ = await _charges.GetAsync(chargeId, cancellationToken) ?? throw new NotFoundException("Cobrança não encontrada.");
        var payments = await _payments.ListByChargeAsync(chargeId, cancellationToken);
        return payments.Select(payment => new PaymentDto(payment.Id, payment.ChargeId, payment.Amount, payment.PaidOn, payment.Method)).ToArray();
    }

    public async Task<ChargeDto> RegisterPaymentAsync(int chargeId, RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        await _validator.EnsureValid(request, cancellationToken);
        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        var charge = await _charges.GetAsync(chargeId, cancellationToken) ?? throw new NotFoundException("Cobrança não encontrada.");
        var finance = await _settings.GetAsync(cancellationToken);
        var mapped = Map(charge, finance);
        ChargeRules.EnsureCanReceive(mapped.Status, mapped.Balance, request.Amount);
        await _payments.AddAsync(chargeId, MoneyRules.Round(request.Amount), request.PaidOn, request.Method, cancellationToken);

        var updated = await _charges.GetAsync(chargeId, cancellationToken) ?? throw new NotFoundException("Cobrança não encontrada.");
        var recalculated = Map(updated, finance);
        var status = ChargeRules.ResolveStatus(updated.Status, updated.DueDate, _clock.Today, recalculated.Balance);
        if (status != updated.Status)
        {
            await _charges.UpdateStatusAsync(chargeId, status, cancellationToken);
        }

        return await GetAsync(chargeId, cancellationToken);
    }

    public async Task<ChargeDto> CancelAsync(int chargeId, CancellationToken cancellationToken)
    {
        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        var charge = await _charges.GetAsync(chargeId, cancellationToken) ?? throw new NotFoundException("Cobrança não encontrada.");
        var finance = await _settings.GetAsync(cancellationToken);
        var mapped = Map(charge, finance);
        ChargeRules.EnsureCanCancel(mapped.Status, mapped.AmountPaid);
        await _charges.UpdateStatusAsync(chargeId, ChargeStatus.Cancelled, cancellationToken);
        return await GetAsync(chargeId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChargeDto>> ListForReportAsync(
        int? clientId,
        ChargeStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await _charges.MarkOverdueAsync(_clock.Today, cancellationToken);
        var finance = await _settings.GetAsync(cancellationToken);
        var charges = await _charges.ListAllAsync(new ChargeFilter(1, int.MaxValue, clientId, status, from, to), cancellationToken);
        return charges.Select(charge => Map(charge, finance)).ToArray();
    }

    private ChargeDto Map(ChargeRecord charge, FinanceSettings finance)
    {
        var amounts = charge.Status == ChargeStatus.Cancelled
            ? new ChargeAmounts(0, 0, charge.OriginalAmount, 0)
            : ChargeRules.Calculate(charge.OriginalAmount, charge.DueDate, _clock.Today, finance.FinePercent, finance.MonthlyInterestPercent);

        var balance = charge.Status == ChargeStatus.Cancelled
            ? 0
            : ChargeRules.Balance(amounts.UpdatedAmount, charge.AmountPaid);

        var status = ChargeRules.ResolveStatus(charge.Status, charge.DueDate, _clock.Today, balance);
        return new ChargeDto(
            charge.Id,
            charge.ContractId,
            charge.ClientId,
            charge.ClientName,
            charge.ContractDescription,
            charge.OriginalAmount,
            charge.DueDate,
            status,
            charge.Notes,
            amounts.DaysLate,
            amounts.Fine,
            amounts.Interest,
            amounts.UpdatedAmount,
            charge.AmountPaid,
            balance);
    }
}
