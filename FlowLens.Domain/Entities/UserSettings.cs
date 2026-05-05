namespace FlowLens.Domain.Entities;

public class UserSettings
{
    public AnalysisPreferences Analysis { get; set; } = new();
    public GraphicsPreferences Graphics { get; set; } = new();
    public DataPreferences Data { get; set; } = new();
}

public class AnalysisPreferences
{
    public List<string> ExcludedFolders { get; set; } = new() { "bin", "obj", "Tests", "Migrations", "node_modules" };
    public int MaxAnalysisDepth { get; set; } = 3;
    public bool ShowExternalLibs { get; set; } = false;
    public int MaxDepth { get; set; }
}

public class GraphicsPreferences
{
    public string NodeDetailLevel { get; set; } = "Detailed"; 
    public bool HighPerformanceMode { get; set; } = false;
    public bool ShowMinimap { get; set; } = true;
}

public class DataPreferences
{
    public string RepoVisibility { get; set; } = "All"; 
}