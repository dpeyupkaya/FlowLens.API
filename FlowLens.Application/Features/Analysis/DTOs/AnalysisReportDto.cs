namespace FlowLens.Application.Features.Analysis.DTOs;

public record RepoStatsDto(
    int Stars,
    int Forks,
    int OpenIssues,
    string PrimaryLanguage,
    DateTime CreatedAt,
    DateTime LastPushedAt,
    string DefaultBranch
);

public record AnalysisReportDto(
    string RepoUrl,
    int TotalFilesScanned,
    int TotalLinesOfCode,
    CodeGraphDto Graph,
    List<string> Issues,
    RepoStatsDto? RepoStats = null 
);