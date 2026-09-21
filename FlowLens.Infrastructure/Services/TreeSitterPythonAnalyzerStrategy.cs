using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Domain.Entities;
using FlowLens.Infrastructure.Analysis.Helpers;
using FlowLens.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure.Services;

public class TreeSitterPythonAnalyzerStrategy : IProjectAnalyzerStrategy
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public string SupportedLanguage => "Python";

    public TreeSitterPythonAnalyzerStrategy(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    private async Task SendLog(string analysisId, string message)
    {
        try
        {
            await _hubContext.Clients.Group(analysisId).SendAsync("ReceiveAnalysisLog", message);
        }
        catch { }
    }

    public async Task<CodeGraphDto> AnalyzeStructureAsync(string analysisId, string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings = null)
    {
        await SendLog(analysisId, $"[SİSTEM] Python Gelişmiş Analiz motoru başlatıldı. (Derinlik Seviyesi: {maxDepth})");

        var nodes = new List<NodeDto>();
        var edges = new List<EdgeDto>();

        var pyFiles = Directory.GetFiles(directoryPath, "*.py", SearchOption.AllDirectories)
            .Where(f => !ignoredFolders.Any(ig => f.Replace("\\", "/").Contains($"/{ig}/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (pyFiles.Count == 0)
        {
            return new CodeGraphDto(nodes, edges);
        }

        await SendLog(analysisId, $"[BİLGİ] {pyFiles.Count} adet Python dosyası tespit edildi. Sezgisel (Heuristic) AST çözümlemesi başlıyor...");

        // Regex Patterns
        var classRegex = new Regex(@"^(\s*)class\s+([A-Za-z_][A-Za-z0-9_]*)(?:\s*\((.*?)\))?\s*:", RegexOptions.Multiline);
        var funcRegex = new Regex(@"^(\s*)(?:async\s+)?def\s+([A-Za-z_][A-Za-z0-9_]*)\s*\((.*?)\)(?:\s*->\s*(.*?))?\s*:", RegexOptions.Multiline);
        var propRegex = new Regex(@"^(\s*)(?:self\.)?([A-Za-z_][A-Za-z0-9_]*)\s*:\s*([A-Za-z_][A-Za-z0-9_\[\], \.]*)\s*(?:=|$)", RegexOptions.Multiline);
        var selfPropRegex = new Regex(@"^(\s*)self\.([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$", RegexOptions.Multiline);
        var importRegex = new Regex(@"^\s*(?:import\s+([A-Za-z0-9_\., ]+)|from\s+([A-Za-z0-9_\.]+)\s+import)", RegexOptions.Multiline);
        var decoratorRegex = new Regex(@"^(\s*)@([A-Za-z_][A-Za-z0-9_\.]*)(?:\((.*)\))?", RegexOptions.Multiline);
        var instantiateRegex = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]*)\s*\(");
        var callRegex = new Regex(@"\.\s*([A-Za-z_][A-Za-z0-9_]*)\s*\(");
        var complexityRegex = new Regex(@"\b(if|elif|for|while|and|or|except|match|case)\b");

        var globalEntities = new Dictionary<string, string>(); 
        var classMethods = new Dictionary<string, List<string>>(); 

        int processed = 0;
        foreach (var file in pyFiles)
        {
            processed++;
            if (processed % 5 == 0 || pyFiles.Count < 10)
            {
                await SendLog(analysisId, $"[BİLGİ] Çözümleniyor: {Path.GetFileName(file)} [{processed}/{pyFiles.Count}]");
            }

            var fileName = Path.GetFileName(file);
            var moduleNameRaw = Path.GetFileNameWithoutExtension(file);
            var moduleNodeId = "mod_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            
            globalEntities[moduleNameRaw] = moduleNodeId;
            var layer = LayerDetector.Detect(file);
            
            var moduleMetadata = new Dictionary<string, object>
            {
                { "Layer", layer },
                { "Namespace", fileName },
                { "Methods", new List<MethodInfoDto>() },
                { "Properties", new List<PropertyInfoDto>() },
                { "Frameworks", new List<string>() }
            };
            
            var moduleNode = new NodeDto(moduleNodeId, fileName, "Python Module", 100, moduleMetadata);
            nodes.Add(moduleNode);

            var code = await File.ReadAllTextAsync(file);

            // 1. Imports & Framework Detection
            var importMatches = importRegex.Matches(code);
            var detectedFrameworks = new HashSet<string>();
            foreach (Match match in importMatches)
            {
                var modStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                var modules = modStr.Split(',').Select(m => m.Trim()).Where(m => !string.IsNullOrEmpty(m));
                
                foreach(var mName in modules)
                {
                    var actualModule = mName.Split(' ')[0];
                    if (!string.IsNullOrEmpty(actualModule))
                    {
                        // Framework Detection
                        if (actualModule.Contains("django")) detectedFrameworks.Add("Django");
                        else if (actualModule.Contains("fastapi")) detectedFrameworks.Add("FastAPI");
                        else if (actualModule.Contains("flask")) detectedFrameworks.Add("Flask");
                        else if (actualModule.Contains("sqlalchemy")) detectedFrameworks.Add("SQLAlchemy");
                        else if (actualModule.Contains("pandas") || actualModule.Contains("numpy")) detectedFrameworks.Add("Data Science");

                        var extId = "ext_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        if (settings == null || settings.ShowExternalLibs)
                        {
                            if (!nodes.Any(n => n.Name == actualModule && n.Type == "External Module"))
                            {
                                nodes.Add(new NodeDto(extId, actualModule, "External Module", 20, new Dictionary<string, object> { { "Layer", "External" } }));
                                edges.Add(new EdgeDto(moduleNodeId, extId, "Imports"));
                            }
                            else
                            {
                                var existingExt = nodes.FirstOrDefault(n => n.Name == actualModule && n.Type == "External Module");
                                if (existingExt != null) edges.Add(new EdgeDto(moduleNodeId, existingExt.Id, "Imports"));
                            }
                        }
                    }
                }
            }
            if (detectedFrameworks.Any())
            {
                moduleMetadata["Frameworks"] = detectedFrameworks.ToList();
            }

            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            NodeDto currentClassNode = null;
            string currentClassIndent = "";
            
            string currentMethodName = null;
            int currentMethodLines = 0;
            int currentMethodComplexity = 1;

            string pendingDecorator = null;
            string pendingDecoratorRoute = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Decorator Detection (For API Endpoints)
                var decMatch = decoratorRegex.Match(line);
                if (decMatch.Success)
                {
                    var decName = decMatch.Groups[2].Value.ToLower();
                    if (decName.Contains("get") || decName.Contains("post") || decName.Contains("put") || decName.Contains("delete") || decName.Contains("route") || decName.Contains("api_view"))
                    {
                        pendingDecorator = "API Endpoint";
                        pendingDecoratorRoute = decMatch.Groups[3].Success ? decMatch.Groups[3].Value : null;
                    }
                    continue;
                }

                if (line.TrimStart().StartsWith("#")) continue;

                var currentIndentMatch = Regex.Match(line, @"^(\s*)");
                var currentIndent = currentIndentMatch.Groups[1].Value;
                
                if (currentClassNode != null && currentIndent.Length <= currentClassIndent.Length)
                {
                    currentClassNode = null;
                }

                if (currentMethodName != null)
                {
                    // Still inside method
                    currentMethodLines++;
                    currentMethodComplexity += complexityRegex.Matches(line).Count;
                }

                // 2. Classes & Database Entities
                var classMatch = classRegex.Match(line);
                if (classMatch.Success)
                {
                    currentMethodName = null; // reset method tracker
                    currentClassIndent = classMatch.Groups[1].Value;
                    var className = classMatch.Groups[2].Value;
                    var baseClasses = classMatch.Groups[3].Success ? classMatch.Groups[3].Value : null;

                    string classNodeType = "Class";
                    if (!string.IsNullOrWhiteSpace(baseClasses))
                    {
                        if (baseClasses.Contains("Model") || baseClasses.Contains("Base") || baseClasses.Contains("Document"))
                        {
                            classNodeType = "Database Entity";
                        }
                    }

                    var classId = "class_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    
                    var classMetadata = new Dictionary<string, object>
                    {
                        { "Layer", layer },
                        { "Namespace", fileName },
                        { "Methods", new List<MethodInfoDto>() },
                        { "Properties", new List<PropertyInfoDto>() }
                    };

                    currentClassNode = new NodeDto(classId, className, classNodeType, classNodeType == "Database Entity" ? 90 : 80, classMetadata);
                    nodes.Add(currentClassNode);
                    edges.Add(new EdgeDto(classId, moduleNodeId, "DeclaredIn"));
                    
                    globalEntities[className] = classId;
                    classMethods[className] = new List<string>();

                    if (!string.IsNullOrWhiteSpace(baseClasses) && maxDepth >= 2)
                    {
                        var bases = baseClasses.Split(',').Select(b => b.Trim());
                        foreach (var b in bases)
                        {
                            var baseId = "base_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                            if (!nodes.Any(n => n.Name == b))
                            {
                                nodes.Add(new NodeDto(baseId, b, "Base Class", 50, new Dictionary<string, object> { { "Layer", "Domain" } }));
                            }
                            var targetNode = nodes.FirstOrDefault(n => n.Name == b)?.Id ?? baseId;
                            edges.Add(new EdgeDto(classId, targetNode, "InheritsFrom"));
                        }
                    }
                    continue;
                }

                // 3. Methods & Functions (API Endpoints & Metrics)
                var funcMatch = funcRegex.Match(line);
                if (funcMatch.Success && maxDepth >= 1)
                {
                    currentMethodName = funcMatch.Groups[2].Value;
                    currentMethodLines = 1;
                    currentMethodComplexity = 1;

                    var funcName = currentMethodName;
                    var rawParams = funcMatch.Groups[3].Value;
                    var returnType = funcMatch.Groups[4].Success ? funcMatch.Groups[4].Value.Trim() : "Any";

                    var parameters = rawParams.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p) && p != "self" && p != "cls")
                        .ToList();

                    var methodInfo = new MethodInfoDto(funcName, returnType, parameters, funcName.StartsWith("_") ? "private" : "public");
                    
                    if (currentClassNode != null)
                    {
                        var methodsList = currentClassNode.Metadata["Methods"] as List<MethodInfoDto>;
                        methodsList?.Add(methodInfo);
                        classMethods[currentClassNode.Name].Add(funcName);
                        
                        if (funcName == "__init__" && maxDepth >= 2)
                        {
                            foreach (var p in parameters)
                            {
                                var pName = p;
                                if (p.Contains(":")) pName = p.Split(':')[1].Trim();
                                
                                if (pName != "int" && pName != "str" && pName != "bool" && pName != "float" && pName != "list" && pName != "dict" && pName != "Any" && pName != "None")
                                {
                                    edges.Add(new EdgeDto(currentClassNode.Id, pName, "DependsOn"));
                                }
                            }
                        }
                    }
                    else
                    {
                        var moduleMethodsList = moduleNode.Metadata["Methods"] as List<MethodInfoDto>;
                        moduleMethodsList?.Add(methodInfo);
                        
                        var funcId = "func_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        globalEntities[funcName] = funcId; 
                        
                        var funcType = pendingDecorator == "API Endpoint" ? "API Endpoint" : "Function";
                        var fMeta = new Dictionary<string, object> { { "Layer", layer } };
                        if (pendingDecoratorRoute != null) fMeta["Route"] = pendingDecoratorRoute;

                        nodes.Add(new NodeDto(funcId, funcName, funcType, funcType == "API Endpoint" ? 70 : 40, fMeta));
                        edges.Add(new EdgeDto(funcId, moduleNodeId, "DeclaredIn"));
                    }
                    
                    pendingDecorator = null;
                    pendingDecoratorRoute = null;
                    continue;
                }

                // 4. Properties (Variables & DB Relations)
                if (maxDepth >= 2)
                {
                    var pMatch = propRegex.Match(line);
                    var spMatch = selfPropRegex.Match(line);
                    
                    string propName = null;
                    string propType = "Any";
                    string propValue = null;

                    if (pMatch.Success)
                    {
                        propName = pMatch.Groups[2].Value;
                        propType = pMatch.Groups[3].Value;
                    }
                    else if (spMatch.Success && currentClassNode != null)
                    {
                        propName = spMatch.Groups[2].Value;
                        propValue = spMatch.Groups[3].Value;
                    }

                    if (propName != null && currentClassNode != null)
                    {
                        var propInfo = new PropertyInfoDto(propName, propType, propName.StartsWith("_") ? "private" : "public");
                        var propsList = currentClassNode.Metadata["Properties"] as List<PropertyInfoDto>;
                        if (!propsList.Any(p => p.Name == propName)) propsList.Add(propInfo);

                        // Database Relation Map
                        if (currentClassNode.Type == "Database Entity" && propValue != null)
                        {
                            if (propValue.Contains("ForeignKey") || propValue.Contains("relationship"))
                            {
                                var relMatch = Regex.Match(propValue, @"(?:ForeignKey|relationship)\s*\(\s*['""]?([A-Za-z_][A-Za-z0-9_]*)['""]?");
                                if (relMatch.Success)
                                {
                                    var targetTable = relMatch.Groups[1].Value;
                                    if (globalEntities.TryGetValue(targetTable, out var tableId))
                                    {
                                        edges.Add(new EdgeDto(currentClassNode.Id, tableId, "HasRelation"));
                                    }
                                }
                            }
                        }
                    }
                }

                // 5. Instantiation and Calls
                if (maxDepth >= 3)
                {
                    var sourceId = currentClassNode != null ? currentClassNode.Id : moduleNodeId;
                    
                    var instMatches = instantiateRegex.Matches(line);
                    foreach (Match match in instMatches)
                    {
                        var called = match.Groups[1].Value;
                        if (called == "print" || called == "len" || called == "range" || called == "super" || called == "int" || called == "str" || called == "list" || called == "dict" || called == "set" || called == "float" || called == "Exception") continue;
                        
                        if (globalEntities.TryGetValue(called, out var targetId))
                        {
                            edges.Add(new EdgeDto(sourceId, targetId, "Instantiates"));
                        }
                    }

                    var cMatches = callRegex.Matches(line);
                    foreach (Match match in cMatches)
                    {
                        var called = match.Groups[1].Value;
                        if (globalEntities.TryGetValue(called, out var targetId))
                        {
                            edges.Add(new EdgeDto(sourceId, targetId, "Calls"));
                        }
                    }
                }
            }
        }

        await SendLog(analysisId, "[BİLGİ] Yapısal bütünlük kontrolü ve akıllı filtreleme devrede...");

        var distinctNodes = nodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = edges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(analysisId, $"[BAŞARI] Analiz başarıyla tamamlandı. Python haritası {distinctNodes.Count} düğüm ile hazır.");

        return new CodeGraphDto(distinctNodes, distinctEdges);
    }
}
