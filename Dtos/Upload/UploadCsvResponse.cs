namespace FinancialIntelligence.Api.Dtos.Upload;

public sealed class UploadCsvResponse
{
    public string Message { get; set; } = default!;
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; } = default!;
    public int RowsInFile { get; set; }
    public int RowsStaged { get; set; }
    public int RowsInserted { get; set; }
    public int RowsSkippedAsDuplicates { get; set; }
    public string Status { get; set; } = default!;
    public int InsightCount { get; set; }
}