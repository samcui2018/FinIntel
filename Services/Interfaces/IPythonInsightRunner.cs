using System.Threading;
using System.Threading.Tasks;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services
{
    public interface IPythonInsightRunner
    {
        Task<PythonInsightProcessResult> RunAsync(
            string scriptKey,
            string businessId,
            string asOfDate,
            int monthsBack,
            CancellationToken cancellationToken = default);
    }
}