namespace FlowLens.Application.Features.Users.DTOs;

public class UserSettingsDto
{
    public AnalysisPreferencesDto Analysis { get; set; } = new();
    public GraphicsPreferencesDto Graphics { get; set; } = new();
    public DataPreferencesDto Data { get; set; } = new();
}

public class AnalysisPreferencesDto
{
    public List<string> ExcludedFolders { get; set; } = new();
    public int MaxAnalysisDepth { get; set; }
    public bool ShowExternalLibs { get; set; }
}

public class GraphicsPreferencesDto
{
    public string NodeDetailLevel { get; set; } = string.Empty;
    public bool HighPerformanceMode { get; set; }
    public bool ShowMinimap { get; set; }
}

public class DataPreferencesDto
{
    public string RepoVisibility { get; set; } = string.Empty;
}