namespace FlowLens.Application.Features.Analysis.DTOs;

public record AnalysisReportDto(
    string RepoUrl,
    int TotalFilesScanned,
    int TotalLinesOfCode,
    CodeGraphDto Graph,
    List<string> Issues
);