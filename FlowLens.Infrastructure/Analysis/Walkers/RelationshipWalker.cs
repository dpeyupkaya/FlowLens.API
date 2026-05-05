using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;
using System.Collections.Generic;

namespace FlowLens.Infrastructure.Analysis.Walkers
{
    public class RelationshipWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly int _maxDepth; 

        public List<EdgeDto> Edges { get; } = new();

        private readonly Stack<string> _classStack = new();
        private readonly Stack<string> _methodStack = new();

        public RelationshipWalker(SemanticModel semanticModel, int maxDepth)
        {
            _semanticModel = semanticModel;
            _maxDepth = maxDepth;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var classSymbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
            if (classSymbol == null) return;

            var classId = classSymbol.ToDisplayString();
            _classStack.Push(classId);

            if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType == SpecialType.None)
            {
                Edges.Add(new EdgeDto(classId, classSymbol.BaseType.ToDisplayString(), "Inherits"));
            }

            foreach (var iface in classSymbol.Interfaces)
            {
                Edges.Add(new EdgeDto(classId, iface.ToDisplayString(), "Implements"));
            }

            if (_maxDepth >= 2)
            {
                base.VisitClassDeclaration(node);
            }

            _classStack.Pop();
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (_maxDepth < 2) return;

            if (_classStack.TryPeek(out var currentClassId))
            {
                var constructorSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
                if (constructorSymbol != null)
                {
                    foreach (var parameter in constructorSymbol.Parameters)
                    {
                        if (parameter.Type.SpecialType == SpecialType.None)
                        {
                            Edges.Add(new EdgeDto(currentClassId, parameter.Type.ToDisplayString(), "DependsOn"));
                        }
                    }
                }
            }

            if (_maxDepth >= 3)
            {
                base.VisitConstructorDeclaration(node);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (_maxDepth < 2) return;

            var methodSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
            if (methodSymbol != null)
            {
                _methodStack.Push(methodSymbol.ToDisplayString());
            }

            if (_maxDepth >= 3)
            {
                base.VisitMethodDeclaration(node);
            }

            if (methodSymbol != null)
            {
                _methodStack.Pop();
            }
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (_maxDepth < 3) return; 

            if (_classStack.TryPeek(out var currentClassId))
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node.Type);
                var typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;

                if (typeSymbol != null && typeSymbol.SpecialType == SpecialType.None)
                {
                    var dependencyId = typeSymbol.ToDisplayString();
                    if (currentClassId != dependencyId)
                    {
                        Edges.Add(new EdgeDto(currentClassId, dependencyId, "Instantiates"));
                    }
                }
            }
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (_maxDepth < 3) return; 

            if (_methodStack.TryPeek(out var callingMethodId))
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                var invokedMethodSymbol = symbolInfo.Symbol as IMethodSymbol;

                if (invokedMethodSymbol != null && invokedMethodSymbol.ContainingType.SpecialType == SpecialType.None)
                {
                    var invokedMethodId = invokedMethodSymbol.ToDisplayString();

                    Edges.Add(new EdgeDto(callingMethodId, invokedMethodId, "Calls"));
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}