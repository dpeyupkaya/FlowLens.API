using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Domain.Entities; 

namespace FlowLens.Application.Interfaces.Infrastructure;

public interface ICodeAnalyzerService
{


    Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings = null);
}
