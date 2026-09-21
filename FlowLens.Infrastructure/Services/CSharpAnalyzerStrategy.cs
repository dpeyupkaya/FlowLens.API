using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Core;
using FlowLens.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure.Services;

public class CSharpAnalyzerStrategy : IProjectAnalyzerStrategy
{
    private readonly RoslynAnalyzerEngine _engine;

    public string SupportedLanguage => "CSharp";

    public CSharpAnalyzerStrategy(RoslynAnalyzerEngine engine)
    {
        _engine = engine;
    }

    public async Task<CodeGraphDto> AnalyzeStructureAsync(string analysisId, string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings = null)
    {
        var result = await _engine.AnalyzeAsync(analysisId, directoryPath, ignoredFolders, maxDepth, settings);

        if (result.Nodes.Count > 5000)
        {
            var limitedNodes = result.Nodes.Take(5000).ToList();
            return new CodeGraphDto(limitedNodes, result.Edges);
        }

        return result;
    }
}