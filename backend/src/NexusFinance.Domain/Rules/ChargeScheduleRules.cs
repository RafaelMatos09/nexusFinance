using NexusFinance.Domain.Enums;

namespace NexusFinance.Domain.Rules;

public static class ChargeScheduleRules
{
    public static IReadOnlyList<DateOnly> ListDueDates(
        DateOnly start,
        DateOnly? end,
        BillingFrequency frequency,
        DateOnly today)
    {
        var step = frequency switch
        {
            BillingFrequency.Quarterly => 3,
            BillingFrequency.Yearly => 12,
            _ => 1
        };

        var dates = new List<DateOnly>();
        var limit = end ?? today;
        for (var date = start; date <= limit; date = date.AddMonths(step))
        {
            dates.Add(date);
        }

        if (end is null)
        {
            var upcoming = start;
            while (upcoming <= today)
            {
                upcoming = upcoming.AddMonths(step);
            }

            if (!dates.Contains(upcoming))
            {
                dates.Add(upcoming);
            }
        }

        return dates;
    }
}
