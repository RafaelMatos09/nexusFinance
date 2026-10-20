using ClosedXML.Excel;
using NexusFinance.Application.Reports;

namespace NexusFinance.Infrastructure.Reporting;

public sealed class ClosedXmlReportExporter : IReportWorkbookExporter
{
    public byte[] Export(ChargeReportDto report)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.AddWorksheet("DASHBOARD");
        summary.Cell(1, 1).Value = "Indicador";
        summary.Cell(1, 2).Value = "Valor";
        WriteRow(summary, 2, "Quantidade", report.Count);
        WriteRow(summary, 3, "Valor original", report.OriginalAmount);
        WriteRow(summary, 4, "Encargos", report.ChargesAmount);
        WriteRow(summary, 5, "Valor atualizado", report.UpdatedAmount);
        WriteRow(summary, 6, "Recebido", report.AmountPaid);
        WriteRow(summary, 7, "Saldo", report.Balance);
        summary.Columns().AdjustToContents();

        var data = workbook.AddWorksheet("DADOS");
        string[] headers =
        [
            "Id", "Cliente", "Contrato", "Vencimento", "Status", "Valor original",
            "Multa", "Juros", "Valor atualizado", "Pago", "Saldo"
        ];
        for (var column = 0; column < headers.Length; column++)
        {
            data.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < report.Rows.Count; index++)
        {
            var row = report.Rows[index];
            var line = index + 2;
            data.Cell(line, 1).Value = row.Id;
            data.Cell(line, 2).Value = row.ClientName;
            data.Cell(line, 3).Value = row.ContractDescription;
            data.Cell(line, 4).Value = row.DueDate.ToDateTime(TimeOnly.MinValue);
            data.Cell(line, 5).Value = row.Status.ToString();
            data.Cell(line, 6).Value = row.OriginalAmount;
            data.Cell(line, 7).Value = row.Fine;
            data.Cell(line, 8).Value = row.Interest;
            data.Cell(line, 9).Value = row.UpdatedAmount;
            data.Cell(line, 10).Value = row.AmountPaid;
            data.Cell(line, 11).Value = row.Balance;
        }

        data.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteRow(IXLWorksheet sheet, int row, string label, decimal value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = value;
    }

    private static void WriteRow(IXLWorksheet sheet, int row, string label, int value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = value;
    }
}
