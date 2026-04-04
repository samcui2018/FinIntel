namespace FinancialIntelligence.Api.Dtos.Intelligence;

public sealed class SpendForecastDto
{
    public decimal NextMonthForecast { get; set; }
    public decimal TrendSlope { get; set; }
    public bool HasSufficientHistory { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}