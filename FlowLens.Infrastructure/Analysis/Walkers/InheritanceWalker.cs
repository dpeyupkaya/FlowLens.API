using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;
using System.Collections.Generic;

namespace FlowLens.Infrastructure.Analysis.Walkers;

public class InheritanceWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    public List<EdgeDto> Edges { get; } = new();

  
    public InheritanceWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        if (classSymbol != null)
        {
            var classId = classSymbol.ToDisplayString(); 

            if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                var baseClassId = classSymbol.BaseType.ToDisplayString();
                Edges.Add(new EdgeDto(classId, baseClassId, "Inherits"));
            }

            foreach (var iface in classSymbol.Interfaces)
            {
                var interfaceId = iface.ToDisplayString();
                Edges.Add(new EdgeDto(classId, interfaceId, "Implements"));
            }
        }

        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var interfaceSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        if (interfaceSymbol != null)
        {
            var interfaceId = interfaceSymbol.ToDisplayString();

            foreach (var iface in interfaceSymbol.Interfaces)
            {
                var baseInterfaceId = iface.ToDisplayString();
                Edges.Add(new EdgeDto(interfaceId, baseInterfaceId, "Inherits"));
            }
        }

        base.VisitInterfaceDeclaration(node);
    }
}