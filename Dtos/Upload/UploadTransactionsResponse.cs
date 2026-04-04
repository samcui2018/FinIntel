namespace FinancialIntelligence.Api.Dtos.Upload;

public sealed class UploadTransactionsResponse
{
    public Guid LoadId { get; set; }
    public int RowsInFile { get; set; }
    public int RowsStaged { get; set; }
    public int IngestionDuplicates { get; set; }
    public int PossibleDuplicateCharges { get; set; }
    public int RowsPromoted { get; set; }
    public string Status { get; set; } = "";
    public int InsightCount { get; set; }
}