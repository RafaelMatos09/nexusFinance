using NexusFinance.Application.Charges;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Application.Dashboard;

public sealed record MonthlyReceiptDto(int Year, int Month, decimal Amount);

public sealed record AgingBucketDto(string Label, decimal Amount);

public sealed record TopDebtorDto(int ClientId, string ClientName, decimal Amount, int ChargeCount);

public sealed record StatusSliceDto(ChargeStatus Status, int Count, decimal Amount);

public sealed record DashboardDto(
    decimal TotalReceivable,
    decimal ReceivedThisMonth,
    decimal Overdue,
    decimal DelinquencyRate,
    int OpenCharges,
    int OverdueCharges,
    decimal AverageTicket,
    IReadOnlyList<MonthlyReceiptDto> ReceiptsByMonth,
    IReadOnlyList<AgingBucketDto> Aging,
    IReadOnlyList<StatusSliceDto> StatusBreakdown,
    IReadOnlyList<TopDebtorDto> TopDebtors);

public sealed class DashboardService
{
    private readonly ChargeService _charges;
    private readonly Abstractions.IPaymentRepository _payments;
    private readonly Abstractions.IClock _clock;

    public DashboardService(ChargeService charges, Abstractions.IPaymentRepository payments, Abstractions.IClock clock)
    {
        _charges = charges;
        _payments = payments;
        _clock = clock;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var charges = await _charges.ListForReportAsync(null, null, null, null, cancellationToken);
        var open = charges.Where(charge => charge.Status is ChargeStatus.Pending or ChargeStatus.Overdue).ToArray();
        var totalReceivable = open.Sum(charge => charge.Balance);
        var overdue = open.Where(charge => charge.Status == ChargeStatus.Overdue).Sum(charge => charge.Balance);
        var rate = totalReceivable == 0 ? 0 : MoneyRules(overdue / totalReceivable * 100m);

        var today = _clock.Today;
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var monthly = await _payments.SumByMonthAsync(start, today, cancellationToken);
        var receipts = Enumerable.Range(0, 6)
            .Select(offset => start.AddMonths(offset))
            .Select(month => new MonthlyReceiptDto(
                month.Year,
                month.Month,
                monthly.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Amount ?? 0))
            .ToArray();

        var receivedThisMonth = receipts[^1].Amount;
        var aging = new[]
        {
            Bucket("0–30", open, 0, 30),
            Bucket("31–60", open, 31, 60),
            Bucket("61–90", open, 61, 90),
            Bucket("90+", open, 91, int.MaxValue)
        };

        var top = open
            .Where(charge => charge.Status == ChargeStatus.Overdue)
            .GroupBy(charge => new { charge.ClientId, charge.ClientName })
            .Select(group => new TopDebtorDto(group.Key.ClientId, group.Key.ClientName, group.Sum(charge => charge.Balance), group.Count()))
            .OrderByDescending(item => item.Amount)
            .Take(5)
            .ToArray();

        var breakdown = Enum.GetValues<ChargeStatus>()
            .Select(status =>
            {
                var slice = charges.Where(charge => charge.Status == status).ToArray();
                return new StatusSliceDto(status, slice.Length, MoneyRules(slice.Sum(charge => charge.UpdatedAmount)));
            })
            .ToArray();

        var overdueCount = open.Count(charge => charge.Status == ChargeStatus.Overdue);
        var averageTicket = charges.Count == 0 ? 0 : MoneyRules(charges.Average(charge => charge.OriginalAmount));

        return new DashboardDto(totalReceivable, receivedThisMonth, overdue, rate, open.Length, overdueCount, averageTicket, receipts, aging, breakdown, top);
    }

    private static AgingBucketDto Bucket(string label, IEnumerable<ChargeDto> charges, int minDays, int maxDays) =>
        new(label, charges.Where(charge => charge.DaysLate >= minDays && charge.DaysLate <= maxDays).Sum(charge => charge.Balance));

    private static decimal MoneyRules(decimal value) =>
        Domain.Rules.MoneyRules.Round(value);
}
