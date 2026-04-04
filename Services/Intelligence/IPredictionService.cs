using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services.Intelligence;

public interface IPredictionService
{
    Task<SpendForecastDto> ForecastMonthlySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}