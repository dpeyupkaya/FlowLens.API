using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;

namespace FlowLens.Infrastructure.Analysis;

public class CodeStructureWalker : CSharpSyntaxWalker
{
    public List<NodeDto> Nodes { get; } = new();
    public List<EdgeDto> Edges { get; } = new();
    private string? _currentClassId;

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classId = node.Identifier.Text;
        _currentClassId = classId;

        Nodes.Add(new NodeDto(classId, classId, "Class", 25));

        base.VisitClassDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (_currentClassId != null)
        {
            var methodName = node.Identifier.Text;
            var methodId = $"{_currentClassId}.{methodName}";

            Nodes.Add(new NodeDto(methodId, methodName, "Method", 12));

            Edges.Add(new EdgeDto(_currentClassId, methodId, "Contains"));
        }

        base.VisitMethodDeclaration(node);
    }
}