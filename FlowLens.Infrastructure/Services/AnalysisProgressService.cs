using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowLens.Infrastructure.Services;

public class AnalysisProgressService : IAnalysisProgressService
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public AnalysisProgressService(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyAsync(string message)
    { 
        await _hubContext.Clients.All.SendAsync("ReceiveAnalysisLog", message);
    }
}