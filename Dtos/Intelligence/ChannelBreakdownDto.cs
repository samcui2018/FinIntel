namespace FinancialIntelligence.Api.Dtos.Intelligence;
public sealed class ChannelBreakdownDto
{
    public string Channel { get; set; } = string.Empty;
    public decimal CaptureRate { get; set; }
    public decimal QualificationRate { get; set; }
    public decimal TransactionShare { get; set; }
}