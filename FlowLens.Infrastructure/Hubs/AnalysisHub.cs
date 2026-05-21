using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowLens.Infrastructure.SignalR;

[Authorize]
public class AnalysisHub : Hub
{
    public async Task SubscribeToAnalysis(string analysisId)
    {

        await Groups.AddToGroupAsync(Context.ConnectionId, analysisId);
    }

    public async Task UnsubscribeFromAnalysis(string analysisId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, analysisId);
    }
}