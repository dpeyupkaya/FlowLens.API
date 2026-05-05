using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FlowLens.Application.Features.Analysis.DTOs;
using FlowLens.Infrastructure.Analysis.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace FlowLens.Infrastructure.Analysis.Walkers
{
    public class StructureWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly int _maxDepth;

        public List<NodeDto> Nodes { get; } = new();
        public List<EdgeDto> Edges { get; } = new();

        private readonly Stack<NodeDto> _currentNodeStack = new();

        public StructureWalker(SemanticModel semanticModel, int maxDepth)
        {
            _semanticModel = semanticModel;
            _maxDepth = maxDepth;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
            if (symbol == null) return;

            var classNode = CreateTypeNode(symbol, "Class", 25);
            Nodes.Add(classNode);
            _currentNodeStack.Push(classNode);

            base.VisitClassDeclaration(node);

            _currentNodeStack.Pop();
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
            if (symbol == null) return;

            var interfaceNode = CreateTypeNode(symbol, "Interface", 20);
            Nodes.Add(interfaceNode);
            _currentNodeStack.Push(interfaceNode);

            base.VisitInterfaceDeclaration(node);

            _currentNodeStack.Pop();
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
            if (symbol == null) return;

            var recordNode = CreateTypeNode(symbol, "Record", 22);
            Nodes.Add(recordNode);
            _currentNodeStack.Push(recordNode);

            base.VisitRecordDeclaration(node);

            _currentNodeStack.Pop();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (_maxDepth < 2) return;

            if (_currentNodeStack.Count > 0)
            {
                var methodSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;

                if (methodSymbol != null && methodSymbol.MethodKind == MethodKind.Ordinary)
                {
                    var parentNode = _currentNodeStack.Peek();

                    var parameters = methodSymbol.Parameters
                        .Select(p => $"{p.Type.ToDisplayString()} {p.Name}")
                        .ToList();

                    var methodInfo = new MethodInfoDto(
                        methodSymbol.Name,
                        methodSymbol.ReturnType.ToDisplayString(),
                        parameters,
                        methodSymbol.DeclaredAccessibility.ToString().ToLower()
                    );

                    if (!parentNode.Metadata.ContainsKey("Methods"))
                        parentNode.Metadata["Methods"] = new List<MethodInfoDto>();

                    ((List<MethodInfoDto>)parentNode.Metadata["Methods"]).Add(methodInfo);
                }
            }
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (_maxDepth < 3) return;

            if (_currentNodeStack.Count > 0)
            {
                var propertySymbol = _semanticModel.GetDeclaredSymbol(node) as IPropertySymbol;
                if (propertySymbol != null)
                {
                    var parentNode = _currentNodeStack.Peek();
                    var propInfo = new PropertyInfoDto(
                        propertySymbol.Name,
                        propertySymbol.Type.ToDisplayString(),
                        propertySymbol.DeclaredAccessibility.ToString().ToLower()
                    );

                    if (!parentNode.Metadata.ContainsKey("Properties"))
                        parentNode.Metadata["Properties"] = new List<PropertyInfoDto>();

                    ((List<PropertyInfoDto>)parentNode.Metadata["Properties"]).Add(propInfo);
                }
            }
            base.VisitPropertyDeclaration(node);
        }

        private NodeDto CreateTypeNode(INamedTypeSymbol symbol, string typeName, int size)
        {
            var id = symbol.ToDisplayString();
            var name = symbol.Name;
            var currentNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "Global";

            var metadata = new Dictionary<string, object>
            {
                { "Layer", LayerDetector.Detect(currentNamespace) },
                { "Namespace", currentNamespace },
                { "Methods", new List<MethodInfoDto>() },
                { "Properties", new List<PropertyInfoDto>() }
            };

            return new NodeDto(id, name, typeName, size, metadata);
        }
    }
}