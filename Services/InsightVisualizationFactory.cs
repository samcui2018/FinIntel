using System.Collections.Generic;
using System.Linq;
using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services;

public static class InsightVisualizationFactory
{
    public static InsightVisualizationDto CreateLineChart(
        string title,
        IEnumerable<string> labels,
        IEnumerable<int>? highlightIndexes = null,
        params InsightVisualizationSeriesDto[] series)
    {
        return new InsightVisualizationDto
        {
            ChartType = "line",
            Title = title,
            Labels = labels.ToList(),
            Series = series.ToList(),
            HighlightIndexes = highlightIndexes?.ToList() ?? new List<int>()
        };
    }

    public static InsightVisualizationDto CreateBarChart(
        string title,
        IEnumerable<string> labels,
        params InsightVisualizationSeriesDto[] series)
    {
        return new InsightVisualizationDto
        {
            ChartType = "bar",
            Title = title,
            Labels = labels.ToList(),
            Series = series.ToList()
        };
    }

    public static InsightVisualizationDto CreateComparisonBarChart(
        string title,
        IEnumerable<string> labels,
        params InsightVisualizationSeriesDto[] series)
    {
        return new InsightVisualizationDto
        {
            ChartType = "comparison-bar",
            Title = title,
            Labels = labels.ToList(),
            Series = series.ToList()
        };
    }
}