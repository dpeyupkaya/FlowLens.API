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

public class JavaScriptAnalyzerStrategy : IProjectAnalyzerStrategy
{
    private readonly IAnalysisProgressService _progressService;

    public string SupportedLanguage => "JavaScript";

    public JavaScriptAnalyzerStrategy(IAnalysisProgressService progressService)
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
        await SendLog(analysisId, "[BİLGİ] JavaScript/TypeScript kod analizi başlatılıyor...");

        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();
        var globalEntities = new Dictionary<string, string>();

        var extensions = new[] { "*.js", "*.jsx", "*.ts", "*.tsx", "*.mjs", "*.cjs" };
        var allFiles = new List<string>();

        foreach (var ext in extensions)
        {
            allFiles.AddRange(Directory.GetFiles(directoryPath, ext, SearchOption.AllDirectories));
        }

        var jsFiles = allFiles
            .Where(f => !ignoredFolders.Any(ign => f.Replace("\\", "/").Contains($"/{ign}/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (jsFiles.Count == 0)
        {
            return new CodeGraphDto(nodes, edges);
        }

        await SendLog(analysisId, $"[BİLGİ] {jsFiles.Count} adet JS/TS dosyası tespit edildi. Sezgisel (Heuristic) ayrıştırma başlıyor...");

        // Extended Regex Patterns
        var importRegex = new Regex(@"^\s*import\s+.*from\s+['""]([^'""]+)['""]", RegexOptions.Multiline);
        var requireRegex = new Regex(@"require\s*\(\s*['""]([^'""]+)['""]\s*\)", RegexOptions.Multiline);
        var classRegex = new Regex(@"^\s*(?:export\s+)?(?:default\s+)?class\s+([A-Za-z0-9_]+)(?:\s+extends\s+([A-Za-z0-9_]+))?", RegexOptions.Multiline);
        var funcDeclRegex = new Regex(@"^\s*(?:export\s+)?(?:default\s+)?(?:async\s+)?function\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)", RegexOptions.Multiline);
        var arrowFuncRegex = new Regex(@"^\s*(?:export\s+)?const\s+([A-Za-z0-9_]+)\s*=\s*(?:async\s*)?(?:\([^)]*\)|[A-Za-z0-9_]+)\s*=>", RegexOptions.Multiline);
        var apiEndpointRegex = new Regex(@"\.(get|post|put|delete|patch)\s*\(\s*['""]([^'""]+)['""]", RegexOptions.Multiline);
        var instantiationRegex = new Regex(@"new\s+([A-Za-z0-9_]+)\s*\(", RegexOptions.Multiline);

        var tempDependencies = new List<(string SourceId, string TargetName, string Type)>();

        int processed = 0;
        foreach (var file in jsFiles)
        {
            processed++;
            if (processed % 10 == 0 || jsFiles.Count < 20)
            {
                await SendLog(analysisId, $"[BİLGİ] Derin analiz: {Path.GetFileName(file)} [{processed}/{jsFiles.Count}]");
            }

            var fileName = Path.GetFileName(file);
            var moduleNameRaw = Path.GetFileNameWithoutExtension(file);
            var moduleNodeId = "mod_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            globalEntities[moduleNameRaw] = moduleNodeId;

            var moduleMetadata = new Dictionary<string, object>
            {
                { "Layer", "Frontend/Node" },
                { "Namespace", fileName },
                { "Methods", new List<MethodInfoDto>() },
                { "Properties", new List<PropertyInfoDto>() },
                { "Frameworks", new List<string>() }
            };

            var moduleNode = new NodeDto(moduleNodeId, fileName, "Module", 100, moduleMetadata);
            nodes.Add(moduleNode);

            var code = await File.ReadAllTextAsync(file);
            
            // 1. Detect Imports & Frameworks
            var importMatches = importRegex.Matches(code).Concat(requireRegex.Matches(code));
            var detectedFrameworks = new HashSet<string>();

            foreach (Match match in importMatches)
            {
                var modStr = match.Groups[1].Value;
                
                if (modStr.Contains("react")) detectedFrameworks.Add("React");
                else if (modStr.Contains("vue")) detectedFrameworks.Add("Vue");
                else if (modStr.Contains("@angular")) detectedFrameworks.Add("Angular");
                else if (modStr.Contains("express")) detectedFrameworks.Add("Express");
                else if (modStr.Contains("next")) detectedFrameworks.Add("Next.js");
                else if (modStr.Contains("nestjs")) detectedFrameworks.Add("NestJS");
                else if (modStr.Contains("redux") || modStr.Contains("zustand")) detectedFrameworks.Add("State Management");

                if (modStr.StartsWith(".") || modStr.StartsWith("/"))
                {
                    var importedName = Path.GetFileNameWithoutExtension(modStr);
                    tempDependencies.Add((moduleNodeId, importedName, "Imports"));
                }
                else if (settings == null || settings.ShowExternalLibs)
                {
                    var extId = "ext_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    if (!nodes.Any(n => n.Name == modStr && n.Type == "External Package"))
                    {
                        nodes.Add(new NodeDto(extId, modStr, "External Package", 20, new Dictionary<string, object> { { "Layer", "External" } }));
                        edges.Add(new EdgeDto(moduleNodeId, extId, "Imports"));
                    }
                    else
                    {
                        var existingExt = nodes.FirstOrDefault(n => n.Name == modStr && n.Type == "External Package");
                        if (existingExt != null) edges.Add(new EdgeDto(moduleNodeId, existingExt.Id, "Imports"));
                    }
                }
            }

            if (detectedFrameworks.Any())
            {
                moduleMetadata["Frameworks"] = detectedFrameworks.ToList();
            }

            // 2. Detect Classes (Controllers, Services, Components)
            var classMatches = classRegex.Matches(code);
            foreach (Match match in classMatches)
            {
                var className = match.Groups[1].Value;
                var baseClass = match.Groups[2].Success ? match.Groups[2].Value : null;

                var classNodeType = "Class";
                if (className.EndsWith("Controller")) classNodeType = "Controller";
                else if (className.EndsWith("Service")) classNodeType = "Service";
                else if (!string.IsNullOrEmpty(baseClass) && (baseClass.Contains("Component"))) classNodeType = "UI Component";

                var classId = "class_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                globalEntities[className] = classId;

                nodes.Add(new NodeDto(classId, className, classNodeType, classNodeType == "Controller" ? 85 : 80, new Dictionary<string, object>
                {
                    { "Layer", "Frontend/Node" },
                    { "File", fileName }
                }));

                edges.Add(new EdgeDto(classId, moduleNodeId, "DeclaredIn"));

                if (!string.IsNullOrEmpty(baseClass) && maxDepth >= 2)
                {
                    tempDependencies.Add((classId, baseClass, "InheritsFrom"));
                }
            }

            // 3. Detect Functions, Arrow Functions & API Endpoints
            if (maxDepth >= 1)
            {
                var funcMatches = funcDeclRegex.Matches(code).Cast<Match>().Concat(arrowFuncRegex.Matches(code).Cast<Match>());
                foreach (var match in funcMatches)
                {
                    var funcName = match.Groups[1].Value;
                    var rawParams = match.Groups.Count > 2 ? match.Groups[2].Value : "";
                    var parameters = rawParams.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
                    
                    var funcNodeType = "Function";
                    if (funcName.StartsWith("use")) funcNodeType = "React Hook";
                    else if (char.IsUpper(funcName[0]) && (detectedFrameworks.Contains("React") || detectedFrameworks.Contains("Next.js"))) 
                        funcNodeType = "UI Component";

                    var funcId = "func_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    globalEntities[funcName] = funcId;

                    nodes.Add(new NodeDto(funcId, funcName, funcNodeType, funcNodeType == "UI Component" ? 70 : 40, new Dictionary<string, object>
                    {
                        { "Layer", "Frontend/Node" },
                        { "Parameters", parameters }
                    }));

                    edges.Add(new EdgeDto(funcId, moduleNodeId, "DeclaredIn"));
                    
                    // Add parameters to module metadata
                    var moduleMethodsList = moduleNode.Metadata["Methods"] as List<MethodInfoDto>;
                    moduleMethodsList?.Add(new MethodInfoDto(funcName, "Any", parameters, "public"));
                }

                // API Endpoints (Express/Axios/Router)
                var apiMatches = apiEndpointRegex.Matches(code);
                foreach (Match match in apiMatches)
                {
                    var httpMethod = match.Groups[1].Value.ToUpper();
                    var route = match.Groups[2].Value;
                    var endpointName = $"{httpMethod} {route}";

                    var endpointId = "api_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    
                    nodes.Add(new NodeDto(endpointId, endpointName, "API Endpoint", 90, new Dictionary<string, object>
                    {
                        { "Route", route },
                        { "Method", httpMethod }
                    }));

                    edges.Add(new EdgeDto(moduleNodeId, endpointId, "HostsRoute"));
                }
            }

            // 4. Detect Instantiations (Dependencies)
            if (maxDepth >= 2)
            {
                var instMatches = instantiationRegex.Matches(code);
                foreach (Match match in instMatches)
                {
                    var instantiatedClass = match.Groups[1].Value;
                    if (instantiatedClass != "Error" && instantiatedClass != "Date" && instantiatedClass != "Promise")
                    {
                        tempDependencies.Add((moduleNodeId, instantiatedClass, "Instantiates"));
                    }
                }
            }
        }

        // Resolve dependencies
        foreach (var dep in tempDependencies)
        {
            if (globalEntities.TryGetValue(dep.TargetName, out var targetId))
            {
                edges.Add(new EdgeDto(dep.SourceId, targetId, dep.Type));
            }
        }

        var distinctNodes = nodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = edges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(analysisId, $"[BAŞARI] Analiz başarıyla tamamlandı. JS/TS haritası {distinctNodes.Count} düğüm ile hazır.");

        return new CodeGraphDto(distinctNodes, distinctEdges);
    }
}
