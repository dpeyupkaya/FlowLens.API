using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Persistence.Services
{
    public class AnalysisLimitService : IAnalysisLimitService
    {
        private readonly AppDbContext _dbContext;

        public AnalysisLimitService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ResetAllDailyLimitsAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Users
                .Where(u => u.DailyAnalysisCount > 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.DailyAnalysisCount, 0),
                    cancellationToken);
        }
    }
}