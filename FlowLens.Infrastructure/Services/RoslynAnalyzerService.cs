using Microsoft.CodeAnalysis.CSharp;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis;

namespace FlowLens.Infrastructure.Services;

public class RoslynAnalyzerService : ICodeAnalyzerService
{
    public async Task<CodeGraphDto> AnalyzeStructureAsync(string directoryPath)
    {
        var allNodes = new List<NodeDto>();
        var allEdges = new List<EdgeDto>();

        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var code = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            var walker = new CodeStructureWalker();
            walker.Visit(root);

            allNodes.AddRange(walker.Nodes);
            allEdges.AddRange(walker.Edges);
        }

        return new CodeGraphDto(
            allNodes.DistinctBy(n => n.Id).ToList(),
            allEdges.DistinctBy(e => new { e.Source, e.Target }).ToList()
        );
    }
}