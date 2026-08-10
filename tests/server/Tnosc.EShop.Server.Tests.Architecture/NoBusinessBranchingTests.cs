// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Tnosc.EShop.Server.Tests.Architecture.Infrastructure;

namespace Tnosc.EShop.Server.Tests.Architecture;

/// <summary>
/// Handlers must not contain business branching — only null tests, Result/error propagation and cancellation checks are permitted.
/// </summary>
public sealed class NoBusinessBranchingTests
{
    private static readonly string[] HandlerProjects =
    [
        "Tnosc.EShop.Server.Application",
        "Tnosc.EShop.Server.Infrastructure.Persistence",
    ];

    [Fact]
    public void HandlerClasses_Should_Not_Contain_Business_Branching()
    {
        List<string> violations = [];

        foreach (string project in HandlerProjects)
        {
            foreach (string file in SourceScanner.EnumerateProjectSourceFiles(projectName: project))
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text: File.ReadAllText(path: file), path: file);
                CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

                foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (!IsHandlerClass(classDeclaration: classDeclaration))
                    {
                        continue;
                    }

                    violations.AddRange(collection: FindViolations(classDeclaration: classDeclaration, file: file));
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Handlers must not contain business branching — only null tests, Result/error propagation and cancellation checks are permitted: {string.Join(separator: "; ", values: violations)}");
    }

    private static bool IsHandlerClass(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList?.Types
            .Any(predicate: baseType => baseType.ToString().Contains(value: "ICommandHandler", comparisonType: System.StringComparison.Ordinal) ||
                              baseType.ToString().Contains(value: "IQueryHandler", comparisonType: System.StringComparison.Ordinal))
        ?? false;

    private static IEnumerable<string> FindViolations(ClassDeclarationSyntax classDeclaration, string file)
    {
        foreach (SyntaxNode node in classDeclaration.DescendantNodes())
        {
            switch (node)
            {
                case IfStatementSyntax ifStatement when !IsAllowedCondition(expression: ifStatement.Condition):
                    yield return Describe(file: file, node: node, kind: "if");
                    break;

                case ConditionalExpressionSyntax conditional when !IsAllowedCondition(expression: conditional.Condition):
                    yield return Describe(file: file, node: node, kind: "ternary (?:)");
                    break;

                case SwitchStatementSyntax:
                    yield return Describe(file: file, node: node, kind: "switch statement");
                    break;

                case SwitchExpressionSyntax:
                    yield return Describe(file: file, node: node, kind: "switch expression");
                    break;
            }
        }
    }

    private static string Describe(string file, SyntaxNode node, string kind)
    {
        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        return $"{file}:{span.StartLinePosition.Line + 1} ({kind})";
    }

    /// <summary>
    /// A condition is allowed when it is purely a null test, a Result/IResult state test, a
    /// CancellationToken.IsCancellationRequested test, or a `&amp;&amp;`/`||`/`!` combination of
    /// those — everything else (domain-value comparisons, enum/status tests, type switches) fails.
    /// </summary>
    private static bool IsAllowedCondition(ExpressionSyntax expression)
    {
        ExpressionSyntax unwrapped = Unparenthesize(expression: expression);

        return unwrapped switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(kind: SyntaxKind.LogicalAndExpression) ||
                                                binary.IsKind(kind: SyntaxKind.LogicalOrExpression)
                => IsAllowedCondition(expression: binary.Left) && IsAllowedCondition(expression: binary.Right),

            PrefixUnaryExpressionSyntax unary when unary.IsKind(kind: SyntaxKind.LogicalNotExpression)
                => IsAllowedCondition(expression: unary.Operand),

            BinaryExpressionSyntax binary when binary.IsKind(kind: SyntaxKind.EqualsExpression) ||
                                                binary.IsKind(kind: SyntaxKind.NotEqualsExpression)
                => IsNullLiteral(expression: binary.Left) || IsNullLiteral(expression: binary.Right),

            IsPatternExpressionSyntax isPattern => IsNullPattern(pattern: isPattern.Pattern),

            MemberAccessExpressionSyntax member => IsStructuralMemberName(name: member.Name.Identifier.Text),

            IdentifierNameSyntax identifier => IsStructuralMemberName(name: identifier.Identifier.Text),

            _ => false,
        };
    }

    private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression) =>
        expression is ParenthesizedExpressionSyntax parenthesized ? Unparenthesize(expression: parenthesized.Expression) : expression;

    private static bool IsNullLiteral(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax literal && literal.IsKind(kind: SyntaxKind.NullLiteralExpression);

    private static bool IsNullPattern(PatternSyntax pattern) => pattern switch
    {
        ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } when literal.IsKind(kind: SyntaxKind.NullLiteralExpression) => true,
        UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } } when literal.IsKind(kind: SyntaxKind.NullLiteralExpression) => true,
        _ => false,
    };

    private static bool IsStructuralMemberName(string name) =>
        name is "IsError" or "IsSuccess" or "IsCancellationRequested";
}
