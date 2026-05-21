using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Domain.Entities;
using FlowLens.Infrastructure.Analysis.Walkers;
using FlowLens.Infrastructure.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure.Analysis.Core;

public class RoslynAnalyzerEngine
{
    private readonly IHubContext<AnalysisHub> _hubContext;

    public RoslynAnalyzerEngine(IHubContext<AnalysisHub> hubContext)
    {
        _hubContext = hubContext;
    }

    private async Task SendLog(string analysisId, string message)
    {
        await _hubContext.Clients.Group(analysisId).SendAsync("ReceiveAnalysisLog", message);
    }

    public async Task<CodeGraphDto> AnalyzeAsync(string analysisId, string directoryPath, List<string> ignoredFolders, int maxDepth, AnalysisPreferences settings)
    {
        await SendLog(analysisId, $"[SİSTEM] Analiz motoru başlatıldı. (Derinlik Seviyesi: {maxDepth})");

        var allNodes = new List<NodeDto>();
        var allEdges = new List<EdgeDto>();
        var allMetrics = new Dictionary<string, (int Complexity, int Lines)>();

        var files = GetProjectFiles(directoryPath, ignoredFolders);

        await SendLog(analysisId, $"[BİLGİ] {files.Count} adet C# dosyası tespit edildi. Bellek içi derleme hazırlanıyor...");

        var syntaxTreeTasks = files.Select(async file =>
        {
            var code = await File.ReadAllTextAsync(file);
            return CSharpSyntaxTree.ParseText(code, path: file);
        });
        var syntaxTrees = (await Task.WhenAll(syntaxTreeTasks)).ToList();

        var compilation = CSharpCompilation.Create("FlowLensCompilation")
            .AddSyntaxTrees(syntaxTrees)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            );

        await SendLog(analysisId, "[BİLGİ] Sanal derleme tamamlandı. Mimari bağlar (Semantic) çözümleniyor...");

        int processedFiles = 0;
        foreach (var tree in syntaxTrees)
        {
            processedFiles++;
            if (processedFiles % 5 == 0 || files.Count < 10)
            {
                await SendLog(analysisId, $"[BİLGİ] Çözümleniyor: {Path.GetFileName(tree.FilePath)} [{processedFiles}/{files.Count}]");
            }

            var root = await tree.GetRootAsync();
            var semanticModel = compilation.GetSemanticModel(tree);

            var structWalker = new StructureWalker(semanticModel, maxDepth);
            structWalker.Visit(root);
            allNodes.AddRange(structWalker.Nodes);
            allEdges.AddRange(structWalker.Edges);

            var relationshipWalker = new RelationshipWalker(semanticModel, maxDepth);
            relationshipWalker.Visit(root);
            allEdges.AddRange(relationshipWalker.Edges);

            if (maxDepth >= 3)
            {
                var metricsWalker = new MetricsWalker(semanticModel);
                metricsWalker.Visit(root);
                foreach (var metric in metricsWalker.MethodMetrics)
                {
                    allMetrics[metric.Key] = metric.Value;
                }
            }
        }

        await SendLog(analysisId, "[BİLGİ] Yapısal bütünlük kontrolü ve akıllı filtreleme devrede...");

        if (settings != null && !settings.ShowExternalLibs)
        {
            var projectNodeIds = new HashSet<string>(allNodes.Select(n => n.Id));
            allEdges.RemoveAll(e => !projectNodeIds.Contains(e.Source) || !projectNodeIds.Contains(e.Target));
        }

        var distinctNodes = allNodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = allEdges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(analysisId, $"[BAŞARI] Analiz başarıyla tamamlandı. Saf mimari haritası {distinctNodes.Count} düğüm ile hazır.");

        return new CodeGraphDto(distinctNodes, distinctEdges);
    }

    private List<string> GetProjectFiles(string directoryPath, List<string> excludedFolders)
    {
        var allFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        if (excludedFolders == null || !excludedFolders.Any())
            return allFiles.ToList();

        return allFiles.Where(file =>
        {
            var normalizedPath = file.Replace("\\", "/");
            return !excludedFolders.Any(folder => normalizedPath.Contains($"/{folder}/", StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }
}