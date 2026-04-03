using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;
using System.Collections.Generic;

namespace FlowLens.Infrastructure.Analysis.Walkers;

public class DependencyWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    public List<EdgeDto> Edges { get; } = new();

    private string? _currentClassId;

    public DependencyWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
        if (classSymbol != null)
        {
            _currentClassId = classSymbol.ToDisplayString();
        }

        base.VisitClassDeclaration(node);

        _currentClassId = classSymbol?.ContainingType?.ToDisplayString();
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (_currentClassId != null)
        {
            var constructorSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
            if (constructorSymbol != null)
            {
                foreach (var parameter in constructorSymbol.Parameters)
                {
                    var dependencyId = parameter.Type.ToDisplayString();

                    if (parameter.Type.SpecialType == SpecialType.None)
                    {
                        Edges.Add(new EdgeDto(_currentClassId, dependencyId, "DependsOn"));
                    }
                }
            }
        }
        base.VisitConstructorDeclaration(node);
    }

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (_currentClassId != null)
        {
            var symbolInfo = _semanticModel.GetSymbolInfo(node.Type);
            var typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;

            if (typeSymbol != null && typeSymbol.SpecialType == SpecialType.None)
            {
                var dependencyId = typeSymbol.ToDisplayString();

                if (_currentClassId != dependencyId)
                {
                    Edges.Add(new EdgeDto(_currentClassId, dependencyId, "Instantiates"));
                }
            }
        }
        base.VisitObjectCreationExpression(node);
    }
}