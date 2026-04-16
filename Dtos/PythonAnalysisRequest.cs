namespace Dtos;
public sealed class PythonAnalysisRequest
{
    public Guid BusinessId { get; set; }
    public Guid LoadId { get; set; }
    public List<PythonTransactionDto> Transactions { get; set; } = new();
}

public sealed class PythonTransactionDto
{
    public DateOnly TransactionDate { get; set; }
    public string? MerchantName { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}