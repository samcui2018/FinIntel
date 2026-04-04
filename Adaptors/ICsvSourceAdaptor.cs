using FinancialIntelligence.Api.Models;
public interface ICsvSourceAdapter
{
    IEnumerable<CanonicalTransaction> Parse(
        Stream stream,
        Guid businessId,
        string sourceFile,
        Guid loadId);
}