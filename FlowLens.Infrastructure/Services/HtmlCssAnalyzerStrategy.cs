using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure.Services;

public class HtmlCssAnalyzerStrategy : IProjectAnalyzerStrategy
{
    private readonly IAnalysisProgressService _progressService;

    public string SupportedLanguage => "HTML_CSS";

    public HtmlCssAnalyzerStrategy(IAnalysisProgressService progressService)
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
        await SendLog(analysisId, "[BİLGİ] HTML/CSS kod analizi başlatılıyor...");

        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();
        var globalEntities = new Dictionary<string, string>();

        var extensions = new[] { "*.html", "*.htm", "*.css", "*.scss", "*.sass", "*.less" };
        var allFiles = new List<string>();

        foreach (var ext in extensions)
        {
            allFiles.AddRange(Directory.GetFiles(directoryPath, ext, SearchOption.AllDirectories));
        }

        var targetFiles = allFiles
            .Where(f => !ignoredFolders.Any(ign => f.Replace("\\", "/").Contains($"/{ign}/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (targetFiles.Count == 0)
        {
            return new CodeGraphDto(nodes, edges);
        }

        await SendLog(analysisId, $"[BİLGİ] {targetFiles.Count} adet HTML/CSS dosyası tespit edildi. Sezgisel (Heuristic) ayrıştırma başlıyor...");

        // Regex Patterns
        var scriptRegex = new Regex(@"<script\s+[^>]*src\s*=\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var linkRegex = new Regex(@"<link\s+[^>]*href\s*=\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var formRegex = new Regex(@"<form\s+[^>]*action\s*=\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var cssImportRegex = new Regex(@"@import\s+(?:url\()?['""]?([^'"")]+)['""]?\)?", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var tempDependencies = new List<(string SourceId, string TargetName, string Type)>();

        int processed = 0;
        foreach (var file in targetFiles)
        {
            processed++;
            if (processed % 10 == 0 || targetFiles.Count < 20)
            {
                await SendLog(analysisId, $"[BİLGİ] Çözümleniyor: {Path.GetFileName(file)} [{processed}/{targetFiles.Count}]");
            }

            var fileName = Path.GetFileName(file);
            var isCss = fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase) || 
                        fileName.EndsWith(".scss", StringComparison.OrdinalIgnoreCase) || 
                        fileName.EndsWith(".sass", StringComparison.OrdinalIgnoreCase) || 
                        fileName.EndsWith(".less", StringComparison.OrdinalIgnoreCase);

            var nodeType = isCss ? "Stylesheet" : "UI View";
            var layer = isCss ? "Frontend/Styles" : "Frontend/Template";
            var nodeId = "ui_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            globalEntities[fileName] = nodeId;
            globalEntities[Path.GetFileNameWithoutExtension(fileName)] = nodeId;

            var metadata = new Dictionary<string, object>
            {
                { "Layer", layer },
                { "File", fileName }
            };

            var fileNode = new NodeDto(nodeId, fileName, nodeType, isCss ? 60 : 90, metadata);
            nodes.Add(fileNode);

            var code = await File.ReadAllTextAsync(file);

            if (!isCss)
            {
                // HTML Analysis
                var scriptMatches = scriptRegex.Matches(code);
                foreach (Match match in scriptMatches)
                {
                    var src = match.Groups[1].Value;
                    var importedName = Path.GetFileName(src);
                    tempDependencies.Add((nodeId, importedName, "IncludesScript"));
                }

                var linkMatches = linkRegex.Matches(code);
                foreach (Match match in linkMatches)
                {
                    var href = match.Groups[1].Value;
                    if (href.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        var importedName = Path.GetFileName(href);
                        tempDependencies.Add((nodeId, importedName, "IncludesStyle"));
                    }
                }

                var formMatches = formRegex.Matches(code);
                foreach (Match match in formMatches)
                {
                    var actionUrl = match.Groups[1].Value;
                    var apiId = "api_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    
                    nodes.Add(new NodeDto(apiId, actionUrl, "API Endpoint", 50, new Dictionary<string, object>
                    {
                        { "Route", actionUrl },
                        { "Type", "Form Action" }
                    }));

                    edges.Add(new EdgeDto(nodeId, apiId, "SubmitsTo"));
                }
            }
            else
            {
                // CSS Analysis
                var importMatches = cssImportRegex.Matches(code);
                foreach (Match match in importMatches)
                {
                    var url = match.Groups[1].Value;
                    var importedName = Path.GetFileName(url);
                    tempDependencies.Add((nodeId, importedName, "ImportsStyle"));
                }
            }
        }

        // Resolve local file dependencies
        foreach (var dep in tempDependencies)
        {
            if (globalEntities.TryGetValue(dep.TargetName, out var targetId))
            {
                edges.Add(new EdgeDto(dep.SourceId, targetId, dep.Type));
            }
            else
            {
                // If it's an external file or a file not in our targetFiles array, we can create a generic external node
                var extId = "ext_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var extType = dep.TargetName.EndsWith(".css") ? "External Style" : "External Script";
                
                nodes.Add(new NodeDto(extId, dep.TargetName, extType, 30, new Dictionary<string, object> { { "Layer", "External/Static" } }));
                edges.Add(new EdgeDto(dep.SourceId, extId, dep.Type));
            }
        }

        var distinctNodes = nodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = edges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(analysisId, $"[BAŞARI] HTML/CSS analizi tamamlandı. {distinctNodes.Count} arayüz öğesi haritalandı.");

        return new CodeGraphDto(distinctNodes, distinctEdges);
    }
}
