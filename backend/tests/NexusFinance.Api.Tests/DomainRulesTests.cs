using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Exceptions;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Api.Tests;

public sealed class DomainRulesTests
{
    [Theory]
    [InlineData("52998224725", true)]
    [InlineData("11111111111", false)]
    [InlineData("11222333000181", true)]
    [InlineData("123", false)]
    public void Document_validation(string document, bool expected)
    {
        Assert.Equal(expected, DocumentoRules.IsValid(document));
    }

    [Theory]
    [InlineData(ContractStatus.Draft, ContractStatus.Active, true)]
    [InlineData(ContractStatus.Draft, ContractStatus.Cancelled, true)]
    [InlineData(ContractStatus.Active, ContractStatus.Closed, true)]
    [InlineData(ContractStatus.Active, ContractStatus.Cancelled, true)]
    [InlineData(ContractStatus.Draft, ContractStatus.Closed, false)]
    [InlineData(ContractStatus.Closed, ContractStatus.Active, false)]
    [InlineData(ContractStatus.Cancelled, ContractStatus.Active, false)]
    public void Contract_transitions(ContractStatus from, ContractStatus to, bool allowed)
    {
        Assert.Equal(allowed, ContractStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Late_fee_and_interest_match_expected_amount()
    {
        var amounts = ChargeRules.Calculate(1000m, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 15), 2m, 1m);

        Assert.Equal(45, amounts.DaysLate);
        Assert.Equal(20m, amounts.Fine);
        Assert.Equal(15m, amounts.Interest);
        Assert.Equal(1035m, amounts.UpdatedAmount);
    }

    [Fact]
    public void On_time_charge_has_no_extra_amount()
    {
        var amounts = ChargeRules.Calculate(1000m, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 1), 2m, 1m);

        Assert.Equal(0, amounts.DaysLate);
        Assert.Equal(0m, amounts.Fine);
        Assert.Equal(1000m, amounts.UpdatedAmount);
    }

    [Fact]
    public void Partial_payment_keeps_balance_and_full_payment_settles()
    {
        var updated = 1035m;
        var partialBalance = ChargeRules.Balance(updated, 500m);
        Assert.Equal(535m, partialBalance);
        Assert.Equal(ChargeStatus.Overdue, ChargeRules.ResolveStatus(ChargeStatus.Overdue, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 15), partialBalance));

        ChargeRules.EnsureCanReceive(ChargeStatus.Overdue, partialBalance, 535m);
        Assert.Equal(ChargeStatus.Paid, ChargeRules.ResolveStatus(ChargeStatus.Overdue, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 15), ChargeRules.Balance(updated, 1035m)));
    }

    [Fact]
    public void Payment_above_balance_is_rejected()
    {
        var error = Assert.Throws<DomainException>(() => ChargeRules.EnsureCanReceive(ChargeStatus.Pending, 100m, 100.01m));
        Assert.Contains("saldo", error.Message);
    }

    [Fact]
    public void Monthly_schedule_includes_past_and_next_due_date()
    {
        var dates = ChargeScheduleRules.ListDueDates(new DateOnly(2026, 1, 10), null, BillingFrequency.Monthly, new DateOnly(2026, 3, 1));

        Assert.Equal(
            new[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 10), new DateOnly(2026, 3, 10) },
            dates);
    }
}
