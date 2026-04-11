namespace FinancialIntelligence.Api.Models;

public class UploadResultDto
{
    public Guid LoadId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ParserType { get; set; } = string.Empty;
    public int RowsRead { get; set; }
    public int RowsInserted { get; set; }
    public int RowsRejected { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}