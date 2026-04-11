using FinancialIntelligence.Api.Models;
using System.Text;
using FinancialIntelligence.Api.Adapters;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Dtos.Analytics;
namespace FinancialIntelligence.Api.Services;

public class AiChatService : IAiChatService
{
    private readonly IGenerativeAiClient _openAiClient;
    private readonly IBusinessAuthorizationService _businessAuthorizationService;
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IInsightRepository _insightRepository;
    private readonly IAnalyticsService _analyticsService;

    public AiChatService(
        IGenerativeAiClient openAiClient,
        IBusinessAuthorizationService businessAuthorizationService,
        IAnalyticsRepository analyticsRepository,
        IInsightRepository insightRepository,
        IAnalyticsService analyticsService)
    {
        _openAiClient = openAiClient;
        _businessAuthorizationService = businessAuthorizationService;
        _analyticsRepository = analyticsRepository;
        _insightRepository = insightRepository;
        _analyticsService = analyticsService;
    }

    public async Task<AiChatResponse> ChatAsync(
        Guid userId,
        Guid businessId,
        AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var hasAccess = await _businessAuthorizationService
            .UserHasAccessToBusinessAsync(userId, businessId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You do not have access to this business.");
        }

        var summary = await _analyticsRepository.GetSpendSummaryAsync(userId,businessId);
        var insights = await _analyticsService.GetTopInsightsAsync(businessId, 5);

        var prompt = BuildPrompt(summary, insights.Insights, request);

        var reply = await _openAiClient.ChatAsync("", prompt, null, cancellationToken);

        return new AiChatResponse
        {
            Reply = reply,
            SuggestedFollowUps = new List<string>
            {
                "Summarize my recent spending trend.",
                "What are the biggest risks in this business?",
                "Which merchants look unusual?"
            }
        };
    }

    private static string BuildPrompt(
        AnalyticsSummaryDto? summary,
        IReadOnlyList<InsightDto> insights,
        AiChatRequest request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are FinIntel, an AI financial analyst for small businesses.");
        sb.AppendLine("Be accurate, concise, practical, and grounded in the provided business data.");
        sb.AppendLine("Do not make up facts. If the data is insufficient, say so.");
        sb.AppendLine();

        sb.AppendLine("Business context:");

        if (summary != null)
        {
            sb.AppendLine($"- Transaction count: {summary.TransactionCount}");
            sb.AppendLine($"- Total amount: {summary.TotalAmount}");
            sb.AppendLine($"- Average amount: {summary.AverageAmount}");
            sb.AppendLine($"- This month amount: {summary.ThisMonthAmount}");
            sb.AppendLine($"- Top merchant: {summary.TopMerchant}");
            sb.AppendLine($"- Latest upload at: {summary.LatestUploadAt}");
        }
        else
        {
            sb.AppendLine("- No summary data available.");
        }

        sb.AppendLine();
        sb.AppendLine("Top insights:");

        if (insights != null && insights.Count > 0)
        {
            foreach (var insight in insights)
            {
                sb.AppendLine($"- {insight.Title}: {insight.Description}");
            }
        }
        else
        {
            sb.AppendLine("- No insights available.");
        }

        sb.AppendLine();

        if (request.History != null && request.History.Count > 0)
        {
            sb.AppendLine("Conversation history:");
            foreach (var turn in request.History.TakeLast(8))
            {
                sb.AppendLine($"{turn.Role}: {turn.Content}");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"User question: {request.Message}");

        return sb.ToString();
    }
}