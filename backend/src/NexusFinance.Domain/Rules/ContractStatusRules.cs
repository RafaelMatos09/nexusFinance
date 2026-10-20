using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Exceptions;

namespace NexusFinance.Domain.Rules;

public static class ContractStatusRules
{
    public static bool CanTransition(ContractStatus from, ContractStatus to) =>
        (from, to) switch
        {
            (ContractStatus.Draft, ContractStatus.Active) => true,
            (ContractStatus.Draft, ContractStatus.Cancelled) => true,
            (ContractStatus.Active, ContractStatus.Closed) => true,
            (ContractStatus.Active, ContractStatus.Cancelled) => true,
            _ => false
        };

    public static void EnsureTransition(ContractStatus from, ContractStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainException($"Transição de {from} para {to} não é permitida.");
        }
    }
}
