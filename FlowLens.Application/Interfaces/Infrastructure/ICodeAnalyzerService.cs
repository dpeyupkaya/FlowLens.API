using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Domain.Entities; 

namespace FlowLens.Application.Interfaces.Infrastructure;

public interface ICodeAnalyzerService
{

    Task<CodeGraphDto> AnalyzeAsync(string path, AnalysisPreferences settings = null);

    Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath, AnalysisPreferences settings = null);
}