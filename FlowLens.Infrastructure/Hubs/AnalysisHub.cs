using Microsoft.AspNetCore.SignalR;

namespace FlowLens.Infrastructure.Hubs;

public class AnalysisHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveAnalysisLog", " Bağlantı stabil.");
        await base.OnConnectedAsync();
    }
}