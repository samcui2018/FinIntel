using ClosedXML.Excel;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Adapters;

public sealed class ExcelSourceAdapter : IExcelSourceAdapter
{
    public IEnumerable<CanonicalTransaction> Parse(
        Stream stream,
        Guid businessId,
        string sourceFile,
        Guid loadId)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            yield break;

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
            yield break;

        var headerRow = DetectHeaderRow(worksheet);
        if (headerRow == null)
            yield break;

        var headerCells = headerRow.CellsUsed().ToList();
        if (headerCells.Count == 0)
            yield break;

        var headers = headerCells
            .Select(c => TransactionRowMapper.NormalizeColumnName(c.GetString()))
            .ToList();

        var sourceRowNumber = 1;
        var startRow = headerRow.RowNumber() + 1;
        var endRow = usedRange.LastRow().RowNumber();

        for (int rowNumber = startRow; rowNumber <= endRow; rowNumber++)
        {
            var rowObj = worksheet.Row(rowNumber);
            if (RowLooksEmpty(rowObj, headerCells.Count))
                continue;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int colIndex = 0; colIndex < headerCells.Count; colIndex++)
            {
                var normalizedHeader = headers[colIndex];
                var value = rowObj.Cell(colIndex + 1).GetFormattedString()?.Trim() ?? string.Empty;
                row[normalizedHeader] = value;
            }

            var transaction = TransactionRowMapper.MapRow(row, businessId, sourceFile, loadId);

            if (transaction != null)
            {
                transaction.SourceRowNumber = sourceRowNumber;
                yield return transaction;
            }

            sourceRowNumber++;
        }
    }

    private static IXLRow? DetectHeaderRow(IXLWorksheet worksheet)
    {
        for (int rowNumber = 1; rowNumber <= 10; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var values = row.CellsUsed()
                .Select(c => TransactionRowMapper.NormalizeColumnName(c.GetString()))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            if (values.Count == 0)
                continue;

            int matched = values.Count(v =>
                TransactionRowMapper.ColumnAliases.Values.Any(aliases => aliases.Contains(v, StringComparer.OrdinalIgnoreCase)));

            if (matched >= 2)
                return row;
        }

        return null;
    }

    private static bool RowLooksEmpty(IXLRow row, int maxColumns)
    {
        for (int i = 1; i <= maxColumns; i++)
        {
            if (!string.IsNullOrWhiteSpace(row.Cell(i).GetFormattedString()))
                return false;
        }

        return true;
    }
}