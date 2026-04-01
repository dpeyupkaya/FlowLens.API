using FlowLens.Application.Features.Analysis.DTOs;

namespace FlowLens.Application.Interfaces.Infrastructure;

public interface ICodeAnalyzerService
{
   
    Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath);
}