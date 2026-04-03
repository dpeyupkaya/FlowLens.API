using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace FlowLens.Infrastructure.Analysis.Walkers;

public class StructureWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;

    public List<NodeDto> Nodes { get; } = new();
    public List<EdgeDto> Edges { get; } = new();

    private string? _currentClassId;

    // 🔥 Motorun gönderdiği "Beyni" (SemanticModel) constructor'dan alıyoruz
    public StructureWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!node.Modifiers.Any(SyntaxKind.PublicKeyword)) return;

        var classSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        if (classSymbol == null) return;

        _currentClassId = classSymbol.ToDisplayString();
        var className = classSymbol.Name;
        var currentNamespace = classSymbol.ContainingNamespace?.ToDisplayString() ?? "Global";

        var layer = LayerDetector.Detect(currentNamespace);

        var metadata = new Dictionary<string, string>
        {
            { "Layer", layer },
            { "Namespace", currentNamespace }
        };

        Nodes.Add(new NodeDto(_currentClassId, className, "Class", 25, metadata));

        base.VisitClassDeclaration(node);

        _currentClassId = classSymbol.ContainingType?.ToDisplayString();
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (_currentClassId != null && node.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            var methodSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
            if (methodSymbol == null) return;

            var methodName = methodSymbol.Name;

            var methodId = methodSymbol.ToDisplayString();

            Nodes.Add(new NodeDto(methodId, methodName, "Method", 12));
            Edges.Add(new EdgeDto(_currentClassId, methodId, "Contains"));

            foreach (var parameter in methodSymbol.Parameters)
            {
                var paramName = parameter.Name;
                var paramType = parameter.Type.ToDisplayString(); 
                var paramId = $"{methodId}.p_{paramName}";

                var metadata = new Dictionary<string, string> { { "DataType", paramType } };

                Nodes.Add(new NodeDto(paramId, paramName, "Parameter", 6, metadata));
                Edges.Add(new EdgeDto(methodId, paramId, "HasParameter"));
            }
        }
        base.VisitMethodDeclaration(node);
    }
}