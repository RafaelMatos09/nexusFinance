namespace NexusFinance.Domain.Rules;

public static class MoneyRules
{
    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
