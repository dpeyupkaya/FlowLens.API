using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces;
using FlowLens.Application.Interfaces.Auth;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Domain.Entities;
using System.Text.RegularExpressions;

namespace FlowLens.Infrastructure.Services;

public class GoAnalyzerStrategy : IProjectAnalyzerStrategy
{
    private readonly IAnalysisProgressService _progressService;

    public string SupportedLanguage => "Go";

    public GoAnalyzerStrategy(IAnalysisProgressService progressService)
    {
        _progressService = progressService;
    }

    private async Task SendLog(string analysisId, string message)
    {
        if (_progressService != null && !string.IsNullOrEmpty(analysisId))
        {
            await _progressService.NotifyAsync(analysisId, message);
        }
    }

    public async Task<CodeGraphDto> AnalyzeStructureAsync(string analysisId, string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings = null)
    {
        await SendLog(analysisId, "[BİLGİ] Go kod analizi başlatılıyor...");

        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();
        var globalEntities = new Dictionary<string, string>();

        var allFiles = Directory.GetFiles(directoryPath, "*.go", SearchOption.AllDirectories)
            .Where(f => !ignoredFolders.Any(ign => f.Replace("\\", "/").Contains($"/{ign}/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var packageRegex = new Regex(@"^package\s+([A-Za-z0-9_]+)");
        var structRegex = new Regex(@"^type\s+([A-Za-z0-9_]+)\s+(struct|interface)");
        var funcRegex = new Regex(@"^func\s+(?:\(\w+\s+\*?([A-Za-z0-9_]+)\)\s+)?([A-Za-z0-9_]+)\s*\(([^)]*)\)");
        var propRegex = new Regex(@"^\s*([A-Za-z0-9_]+)\s+([A-Za-z0-9_\[\]\*\.]+)");
        
        var tempDependencies = new List<(string SourceId, string TargetName)>();

        foreach (var file in allFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            string currentPackage = "main";
            NodeDto currentStructNode = null;
            bool inStruct = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;

                var pkgMatch = packageRegex.Match(trimmed);
                if (pkgMatch.Success)
                {
                    currentPackage = pkgMatch.Groups[1].Value;
                    continue;
                }

                if (trimmed == "}")
                {
                    inStruct = false;
                    currentStructNode = null;
                    continue;
                }

                var structMatch = structRegex.Match(trimmed);
                if (structMatch.Success)
                {
                    var structName = structMatch.Groups[1].Value;
                    var typeName = structMatch.Groups[2].Value == "interface" ? "Interface" : "Class";
                    
                    var structId = "struct_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    globalEntities[structName] = structId;

                    currentStructNode = new NodeDto(structId, structName, typeName, 60, new Dictionary<string, object>
                    {
                        { "Layer", currentPackage },
                        { "Package", currentPackage },
                        { "Methods", new List<MethodInfoDto>() },
                        { "Properties", new List<PropertyInfoDto>() }
                    });
                    
                    nodes.Add(currentStructNode);
                    inStruct = trimmed.EndsWith("{");
                    continue;
                }

                if (inStruct && currentStructNode != null)
                {
                    var propMatch = propRegex.Match(trimmed);
                    if (propMatch.Success)
                    {
                        var propName = propMatch.Groups[1].Value;
                        var propType = propMatch.Groups[2].Value;
                        
                        var propsList = currentStructNode.Metadata["Properties"] as List<PropertyInfoDto>;
                        propsList?.Add(new PropertyInfoDto(propName, propType, char.IsUpper(propName[0]) ? "public" : "private"));

                        var cleanType = propType.Replace("*", "").Replace("[]", "").Split('.').Last();
                        if (cleanType != "string" && cleanType != "int" && cleanType != "bool" && cleanType != "float64")
                        {
                            tempDependencies.Add((currentStructNode.Id, cleanType));
                        }
                    }
                    continue;
                }

                var funcMatch = funcRegex.Match(trimmed);
                if (funcMatch.Success && maxDepth >= 1)
                {
                    var receiver = funcMatch.Groups[1].Success ? funcMatch.Groups[1].Value : null;
                    var funcName = funcMatch.Groups[2].Value;
                    var parameters = funcMatch.Groups[3].Value;

                    var paramList = parameters.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();

                    var methodInfo = new MethodInfoDto(funcName, "Any", paramList, char.IsUpper(funcName[0]) ? "public" : "private");

                    if (receiver != null)
                    {
                        if (globalEntities.TryGetValue(receiver, out var targetStructId))
                        {
                            var structNode = nodes.FirstOrDefault(n => n.Id == targetStructId);
                            if (structNode != null)
                            {
                                var methodsList = structNode.Metadata["Methods"] as List<MethodInfoDto>;
                                methodsList?.Add(methodInfo);
                            }
                        }
                        else
                        {
                            tempDependencies.Add((receiver, funcName)); // Save mapping for later if out of order
                        }
                    }
                    else
                    {
                        var funcId = "func_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        globalEntities[funcName] = funcId;

                        nodes.Add(new NodeDto(funcId, funcName, "Function", 40, new Dictionary<string, object>
                        {
                            { "Layer", currentPackage },
                            { "Package", currentPackage }
                        }));
                    }
                }
            }
        }

        foreach (var dep in tempDependencies)
        {
            if (globalEntities.TryGetValue(dep.TargetName, out var targetId))
            {
                edges.Add(new EdgeDto(dep.SourceId, targetId, "DependsOn"));
            }
        }

        var distinctNodes = nodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = edges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(analysisId, $"[BAŞARI] Analiz başarıyla tamamlandı. Go haritası {distinctNodes.Count} düğüm ile hazır.");

        return new CodeGraphDto(distinctNodes, distinctEdges);
    }
}
