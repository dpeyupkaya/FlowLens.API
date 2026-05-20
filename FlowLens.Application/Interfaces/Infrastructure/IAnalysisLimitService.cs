using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Application.Interfaces.Infrastructure
{
    public interface IAnalysisLimitService
    {
        Task ResetAllDailyLimitsAsync(CancellationToken cancellationToken = default);
    }
}