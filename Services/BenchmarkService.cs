using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Repositories;
namespace FinancialIntelligence.Api.Services;

public sealed class BenchmarkService : IBenchmarkService
{
    private readonly IBenchmarkRepository _repository;
    private readonly ILogger<BenchmarkService> _logger;

    public BenchmarkService(
        IBenchmarkRepository repository,
        ILogger<BenchmarkService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BenchmarkComparisonDto?> CompareAsync(
        Guid businessId,
        string metricKey,
        decimal actualValue,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(businessId));

        if (string.IsNullOrWhiteSpace(metricKey))
            throw new ArgumentException("Metric key is required.", nameof(metricKey));

        var context = await _repository.GetBusinessBenchmarkContextAsync(businessId, cancellationToken);
        if (context is null)
        {
            _logger.LogWarning("No benchmark context found for business {BusinessId}", businessId);
            return null;
        }

        var profiles = await _repository.GetBenchmarkProfilesAsync(cancellationToken);
        if (profiles.Count == 0)
        {
            _logger.LogWarning("No benchmark profiles found.");
            return null;
        }

        var profile = ResolveBestProfile(context, profiles);
        if (profile is null)
        {
            _logger.LogWarning("No benchmark profile could be resolved for business {BusinessId}", businessId);
            return null;
        }

        var metricValue = await _repository.GetBenchmarkMetricValueAsync(
            profile.BenchmarkProfileId,
            metricKey,
            cancellationToken);

        if (metricValue is null)
        {
            _logger.LogWarning(
                "No benchmark metric value found for profile {BenchmarkProfileId} and metric {MetricKey}",
                profile.BenchmarkProfileId,
                metricKey);

            return null;
        }

        var percentile = EstimatePercentile(
            actualValue,
            metricValue.P10,
            metricValue.P25,
            metricValue.P50,
            metricValue.P75,
            metricValue.P90);

        return new BenchmarkComparisonDto
        {
            BusinessMonthlySpend=0,

        };
        // return new BenchmarkComparisonDto
        // {
            // MetricKey = metricKey,
            // ActualValue = actualValue,
            // P10 = metricValue.P10,
            // P25 = metricValue.P25,
            // P50 = metricValue.P50,
            // P75 = metricValue.P75,
            // P90 = metricValue.P90,
            // MeanValue = metricValue.MeanValue,
            // Percentile = percentile,
            // PositionLabel = GetPositionLabel(percentile),
            // BenchmarkProfileId = profile.BenchmarkProfileId,
            // CohortLabel = profile.ProfileName,
            // SourceType = profile.SourceType,
            // ConfidenceScore = profile.ConfidenceScore,
            // SampleSize = metricValue.SampleSize > 0 ? metricValue.SampleSize : profile.SampleSize
        // };
    }

    private static BenchmarkProfile? ResolveBestProfile(
        BusinessBenchmarkContext context,
        IReadOnlyList<BenchmarkProfile> profiles)
    {
        var normalizedIndustry = NormalizeIndustry(context.Industry);
        var spendBand = ResolveSpendBand(context.AverageMonthlySpend);
        var geography = NormalizeText(context.Geography);

        var exact = profiles.FirstOrDefault(p =>
            NormalizeIndustry(p.Industry) == normalizedIndustry &&
            NormalizeText(p.SpendBand) == spendBand &&
            MatchesGeography(p.Geography, geography));

        if (exact is not null)
            return exact;

        var industryOnly = profiles.FirstOrDefault(p =>
            NormalizeIndustry(p.Industry) == normalizedIndustry &&
            MatchesGeography(p.Geography, geography));

        if (industryOnly is not null)
            return industryOnly;

        var generalByBand = profiles.FirstOrDefault(p =>
            IsGeneralIndustry(p.Industry) &&
            NormalizeText(p.SpendBand) == spendBand &&
            MatchesGeography(p.Geography, geography));

        if (generalByBand is not null)
            return generalByBand;

        var general = profiles.FirstOrDefault(p =>
            IsGeneralIndustry(p.Industry) &&
            MatchesGeography(p.Geography, geography));

        return general;
    }

    private static decimal? EstimatePercentile(
        decimal actualValue,
        decimal? p10,
        decimal? p25,
        decimal? p50,
        decimal? p75,
        decimal? p90)
    {
        var points = new List<(decimal Percentile, decimal Value)>();

        if (p10.HasValue) points.Add((10m, p10.Value));
        if (p25.HasValue) points.Add((25m, p25.Value));
        if (p50.HasValue) points.Add((50m, p50.Value));
        if (p75.HasValue) points.Add((75m, p75.Value));
        if (p90.HasValue) points.Add((90m, p90.Value));

        points = points
            .Where(x => x.Value >= 0)
            .OrderBy(x => x.Value)
            .ToList();

        if (points.Count < 2)
            return null;

        if (actualValue <= points[0].Value)
        {
            if (points[0].Value == 0m)
                return points[0].Percentile;

            var scaled = points[0].Percentile * (actualValue / points[0].Value);
            return RoundPercentile(Clamp(scaled, 0m, points[0].Percentile));
        }

        for (var i = 0; i < points.Count - 1; i++)
        {
            var left = points[i];
            var right = points[i + 1];

            if (actualValue >= left.Value && actualValue <= right.Value)
            {
                if (right.Value == left.Value)
                    return RoundPercentile(right.Percentile);

                var ratio = (actualValue - left.Value) / (right.Value - left.Value);
                var interpolated = left.Percentile + ((right.Percentile - left.Percentile) * ratio);
                return RoundPercentile(Clamp(interpolated, 0m, 100m));
            }
        }

        var last = points[^1];
        if (last.Value == 0m)
            return last.Percentile;

        var overflowRatio = (actualValue - last.Value) / last.Value;
        var tailPercentile = last.Percentile + (overflowRatio * 10m);

        return RoundPercentile(Clamp(tailPercentile, last.Percentile, 99m));
    }

    private static string GetPositionLabel(decimal? percentile)
    {
        if (!percentile.HasValue)
            return "benchmark unavailable";

        if (percentile.Value < 25m)
            return "below typical";

        if (percentile.Value <= 75m)
            return "within typical range";

        return "above typical";
    }

    private static string ResolveSpendBand(decimal? averageMonthlySpend)
    {
        if (!averageMonthlySpend.HasValue || averageMonthlySpend.Value <= 0m)
            return "25k-100k";

        var spend = averageMonthlySpend.Value;

        if (spend < 25000m)
            return "0-25k";

        if (spend <= 100000m)
            return "25k-100k";

        return "100k+";
    }

    private static bool MatchesGeography(string? profileGeography, string contextGeography)
    {
        var normalizedProfile = NormalizeText(profileGeography);
        return string.IsNullOrEmpty(normalizedProfile)
            || normalizedProfile == contextGeography;
    }

    private static bool IsGeneralIndustry(string? industry) =>
        string.IsNullOrEmpty(industry) || NormalizeIndustry(industry) == "general";

    private static string NormalizeIndustry(string? value)
    {
        var normalized = NormalizeText(value);

        return normalized switch
        {
            "" => "general",
            "professionalservices" => "professionalservices",
            "professional services" => "professionalservices",
            _ => normalized.Replace(" ", string.Empty)
        };
    }

    private static string NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        Math.Min(max, Math.Max(min, value));

    private static decimal RoundPercentile(decimal value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);
}