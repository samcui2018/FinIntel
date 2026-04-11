using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Adapters;

public interface IExcelSourceAdapter
{
    IEnumerable<CanonicalTransaction> Parse(
        Stream stream,
        Guid businessId,
        string sourceFile,
        Guid loadId);
}