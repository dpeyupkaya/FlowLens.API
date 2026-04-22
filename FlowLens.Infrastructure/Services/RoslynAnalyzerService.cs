using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Core;
using FlowLens.Domain.Entities;

namespace FlowLens.Infrastructure.Services;

public class RoslynAnalyzerService : ICodeAnalyzerService
{
    private readonly RoslynAnalyzerEngine _engine;

    public RoslynAnalyzerService(RoslynAnalyzerEngine engine)
    {
        _engine = engine;
    }
    public async Task<CodeGraphDto> AnalyzeAsync(string path, AnalysisPreferences settings = null)
    {
        return await _engine.AnalyzeAsync(path, settings);
    }

    public async Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath, AnalysisPreferences settings = null)
    {
        var result = await _engine.AnalyzeAsync(directoryPath, settings);

        if (result.Nodes.Count > 5000)
        {
            var limitedNodes = result.Nodes.Take(5000).ToList();
            return new CodeGraphDto(limitedNodes, result.Edges);
        }

        return result;
    }
}