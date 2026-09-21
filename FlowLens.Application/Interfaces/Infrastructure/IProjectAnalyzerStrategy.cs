using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowLens.Application.Interfaces.Infrastructure;

public interface IProjectAnalyzerStrategy
{
    string SupportedLanguage { get; }
    Task<CodeGraphDto> AnalyzeStructureAsync(string analysisId, string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings = null);
}
