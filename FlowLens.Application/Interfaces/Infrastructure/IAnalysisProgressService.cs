namespace FlowLens.Application.Interfaces;

public interface IAnalysisProgressService
{
    Task NotifyAsync(string analysisId, string message);
}   