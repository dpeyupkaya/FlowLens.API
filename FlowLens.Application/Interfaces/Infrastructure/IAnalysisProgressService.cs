
namespace FlowLens.Application.Interfaces.Infrastructure
{
    public interface IAnalysisProgressService
    {
        Task NotifyAsync(string message);
    }
}
