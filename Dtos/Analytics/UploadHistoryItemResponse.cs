namespace FinancialIntelligence.Api.Dtos.Analytics;

public class UploadHistoryItemResponse
{
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; } = Guid.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public int RowsInFile { get; set; }
    public int? RowsInserted { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}