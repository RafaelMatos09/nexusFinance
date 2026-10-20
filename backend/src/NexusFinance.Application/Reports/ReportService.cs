using NexusFinance.Application.Charges;
using NexusFinance.Domain.Enums;

namespace NexusFinance.Application.Reports;

public sealed record ChargeReportDto(
    int Count,
    decimal OriginalAmount,
    decimal ChargesAmount,
    decimal UpdatedAmount,
    decimal AmountPaid,
    decimal Balance,
    IReadOnlyList<ChargeDto> Rows);

public interface IReportWorkbookExporter
{
    byte[] Export(ChargeReportDto report);
}

public sealed class ReportService
{
    private readonly ChargeService _charges;

    public ReportService(ChargeService charges)
    {
        _charges = charges;
    }

    public async Task<ChargeReportDto> GetAsync(
        int? clientId,
        ChargeStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var rows = await _charges.ListForReportAsync(clientId, status, from, to, cancellationToken);
        return new ChargeReportDto(
            rows.Count,
            rows.Sum(row => row.OriginalAmount),
            rows.Sum(row => row.Fine + row.Interest),
            rows.Sum(row => row.UpdatedAmount),
            rows.Sum(row => row.AmountPaid),
            rows.Sum(row => row.Balance),
            rows);
    }
}
