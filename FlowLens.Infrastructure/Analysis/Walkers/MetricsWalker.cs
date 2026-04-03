using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace FlowLens.Infrastructure.Analysis.Walkers;

public class MetricsWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;

    
    public Dictionary<string, (int Complexity, int Lines)> MethodMetrics { get; } = new();

    public MetricsWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodSymbol = _semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
        if (methodSymbol == null) return;

     
        var methodId = methodSymbol.ToDisplayString();

        int complexity = CalculateComplexity(node);

        var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
        int lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;

        MethodMetrics[methodId] = (complexity, lines);

        base.VisitMethodDeclaration(node);
    }

    private int CalculateComplexity(MethodDeclarationSyntax node)
    {
        int count = 1; 
        var tokens = node.DescendantNodesAndTokens();
        foreach (var token in tokens)
        {
            switch (token.Kind())
            {
                case SyntaxKind.IfStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.CaseSwitchLabel:
                case SyntaxKind.DefaultSwitchLabel:
                case SyntaxKind.CatchClause:
                case SyntaxKind.QuestionQuestionToken: 
                case SyntaxKind.AmpersandAmpersandToken: 
                case SyntaxKind.BarBarToken: 
                case SyntaxKind.ConditionalExpression: 
                    count++;
                    break;
            }
        }
        return count;
    }
}