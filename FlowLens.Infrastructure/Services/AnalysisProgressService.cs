using FlowLens.Application.Interfaces;
using FlowLens.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace FlowLens.Infrastructure.Services;

public class AnalysisProgressService : IAnalysisProgressService
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public AnalysisProgressService(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(string analysisId, string message)
    {
        await _hubContext.Clients.Group(analysisId).SendAsync("ReceiveLog", message);
    }
}