using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Core;

namespace FlowLens.Infrastructure.Services;

public class RoslynAnalyzerService : ICodeAnalyzerService
{
    private readonly RoslynAnalyzerEngine _engine;

    public RoslynAnalyzerService(RoslynAnalyzerEngine engine)
    {
        _engine = engine;
    }

    public async Task<CodeGraphDto> AnalyzeAsync(string path)
    {
        return await _engine.AnalyzeAsync(path);
    }

    public async Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath)
    {
        var result = await _engine.AnalyzeAsync(directoryPath);

        if (result.Nodes.Count > 5000)
        {
            var limitedNodes = result.Nodes.Take(5000).ToList();
            return new CodeGraphDto(limitedNodes, result.Edges);
        }

        return result;
    }
}