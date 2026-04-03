using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Walkers;
using FlowLens.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
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

    private async Task SendLog(string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveAnalysisLog", message);
    }

    public async Task<CodeGraphDto> AnalyzeAsync(string directoryPath)
    {
        await SendLog("[SİSTEM] Analiz motoru başlatıldı. Anlamsal (Semantic) derleme aşamasına geçiliyor.");

        var allNodes = new List<NodeDto>();
        var allEdges = new List<EdgeDto>();
        var allMetrics = new Dictionary<string, (int Complexity, int Lines)>();

        var files = GetProjectFiles(directoryPath);
        await SendLog($"[BİLGİ] {files.Count} adet C# dosyası tespit edildi. Bellek içi derleme hazırlanıyor...");

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in files)
        {
            var code = await File.ReadAllTextAsync(file);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: file));
        }

        var compilation = CSharpCompilation.Create("FlowLensCompilation")
            .AddSyntaxTrees(syntaxTrees)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            );

        await SendLog("[BİLGİ] Sanal derleme tamamlandı. Mimari bağlar (Semantic) çözümleniyor...");

        int processedFiles = 0;

        foreach (var tree in syntaxTrees)
        {
            processedFiles++;
            if (processedFiles % 5 == 0 || files.Count < 10)
            {
                await SendLog($"[BİLGİ] Çözümleniyor: {Path.GetFileName(tree.FilePath)} [{processedFiles}/{files.Count}]");
            }

            var root = await tree.GetRootAsync();
            var semanticModel = compilation.GetSemanticModel(tree);

            var structWalker = new StructureWalker(semanticModel);
            structWalker.Visit(root);
            allNodes.AddRange(structWalker.Nodes);
            allEdges.AddRange(structWalker.Edges);

            var depWalker = new DependencyWalker(semanticModel);
            depWalker.Visit(root);
            allEdges.AddRange(depWalker.Edges);

            var inhWalker = new InheritanceWalker(semanticModel);
            inhWalker.Visit(root);
            allEdges.AddRange(inhWalker.Edges);
            var metricsWalker = new MetricsWalker(semanticModel);
            metricsWalker.Visit(root);
            foreach (var metric in metricsWalker.MethodMetrics)
            {
                allMetrics[metric.Key] = metric.Value;
            }
        }

        await SendLog("[BİLGİ] Gürültü ve dış bağımlılıklar filtreleniyor...");

        var forbiddenKeywords = new[] {
            "System", "Microsoft", "Newtonsoft", "Moq", "Xunit", "NUnit", "AutoMapper", "Test"
        };

        allNodes.RemoveAll(n =>
            forbiddenKeywords.Any(k =>
                (n.Metadata?.GetValueOrDefault("Namespace", "") ?? "").StartsWith(k, StringComparison.OrdinalIgnoreCase) ||
                n.Id.Contains(k, StringComparison.OrdinalIgnoreCase)
            )
        );

        allEdges.RemoveAll(e =>
            forbiddenKeywords.Any(k =>
                e.Source.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                e.Target.Contains(k, StringComparison.OrdinalIgnoreCase)
            )
        );

        var distinctNodes = allNodes.DistinctBy(n => n.Id).ToList();
        var distinctEdges = allEdges.DistinctBy(e => new { e.Source, e.Target, e.RelationType }).ToList();

        await SendLog(" Galaksi inşa ediliyor, son rütuşlar yapılıyor...");

        var finalNodes = ResolveMissingNodes(distinctNodes, distinctEdges);

        await SendLog("[BAŞARI] Yapısal ve anlamsal analiz işlemleri sorunsuz tamamlandı.");

        return new CodeGraphDto(finalNodes, distinctEdges);
    }

    private List<string> GetProjectFiles(string directoryPath)
    {
        return Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\Tests\\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains("\\tests\\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains("/tests/", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains("Test.cs", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) &&
                        !f.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private List<NodeDto> ResolveMissingNodes(List<NodeDto> nodes, List<EdgeDto> edges)
    {
        var existingNodeIds = new HashSet<string>(nodes.Select(n => n.Id));
        var missingTargets = edges
            .Where(e => !existingNodeIds.Contains(e.Target))
            .Select(e => e.Target)
            .Distinct()
            .ToList();

        foreach (var targetId in missingTargets)
        {
            nodes.Add(new NodeDto(
                targetId,
                targetId.Split('.').Last(), 
                "ExternalType",
                15,
                new Dictionary<string, string> { { "Layer", "External" } }
            ));
        }

        return nodes;
    }
}