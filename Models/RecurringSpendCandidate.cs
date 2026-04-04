public sealed class RecurringSpendCandidate
{
    public Guid BusinessId { get; set; }
    public string MerchantName { get; set; } = "";
    public int TransactionCount { get; set; }
    public decimal AverageAmount { get; set; }
    public string Frequency { get; set; } = ""; // Monthly, Biweekly, Weekly
    public DateTime FirstTransactionDate { get; set; }
    public DateTime LastTransactionDate { get; set; }
}