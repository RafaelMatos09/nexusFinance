using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Exceptions;

namespace NexusFinance.Domain.Rules;

public readonly record struct ChargeAmounts(decimal Fine, decimal Interest, decimal UpdatedAmount, int DaysLate);

public static class ChargeRules
{
    public static ChargeAmounts Calculate(
        decimal originalAmount,
        DateOnly dueDate,
        DateOnly today,
        decimal finePercent,
        decimal monthlyInterestPercent)
    {
        var daysLate = dueDate < today ? today.DayNumber - dueDate.DayNumber : 0;
        if (daysLate <= 0)
        {
            return new ChargeAmounts(0, 0, MoneyRules.Round(originalAmount), 0);
        }

        var fine = MoneyRules.Round(originalAmount * finePercent / 100m);
        var interest = MoneyRules.Round(originalAmount * (monthlyInterestPercent / 100m) * (daysLate / 30m));
        return new ChargeAmounts(fine, interest, MoneyRules.Round(originalAmount + fine + interest), daysLate);
    }

    public static decimal Balance(decimal updatedAmount, decimal amountPaid) =>
        MoneyRules.Round(updatedAmount - amountPaid);

    public static ChargeStatus ResolveStatus(ChargeStatus current, DateOnly dueDate, DateOnly today, decimal balance)
    {
        if (current == ChargeStatus.Cancelled)
        {
            return ChargeStatus.Cancelled;
        }

        if (balance <= 0)
        {
            return ChargeStatus.Paid;
        }

        return dueDate < today ? ChargeStatus.Overdue : ChargeStatus.Pending;
    }

    public static void EnsureCanReceive(ChargeStatus status, decimal balance, decimal amountPaid)
    {
        if (status == ChargeStatus.Cancelled)
        {
            throw new DomainException("Cobrança cancelada não aceita recebimento.");
        }

        if (status == ChargeStatus.Paid || balance <= 0)
        {
            throw new DomainException("Esta cobrança já está quitada.");
        }

        if (amountPaid <= 0)
        {
            throw new DomainException("O valor pago deve ser maior que zero.");
        }

        if (amountPaid > balance)
        {
            throw new DomainException("O pagamento não pode ser maior que o saldo devido.");
        }
    }

    public static void EnsureCanCancel(ChargeStatus status, decimal amountPaid)
    {
        if (status is ChargeStatus.Cancelled or ChargeStatus.Paid)
        {
            throw new DomainException("Esta cobrança não pode ser cancelada.");
        }

        if (amountPaid > 0)
        {
            throw new DomainException("Cobrança com recebimento não pode ser cancelada.");
        }
    }
}
