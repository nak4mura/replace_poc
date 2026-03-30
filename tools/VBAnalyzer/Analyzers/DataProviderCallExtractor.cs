using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBAnalyzer.Models;

namespace VBAnalyzer.Analyzers;

public class DataProviderCallExtractor : VisualBasicSyntaxWalker
{
    private static readonly HashSet<string> ExecuteMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExecuteReader",
        "ExecuteNonQuery",
        "ExecuteScalar",
        "ExecuteDataSet",
        "ExecuteSQL"
    };

    private readonly List<DataProviderCall> _calls = new();
    private string? _currentNamespace;
    private string? _currentClass;
    private string? _currentMethod;

    public IReadOnlyList<DataProviderCall> Calls => _calls;

    public override void VisitNamespaceBlock(NamespaceBlockSyntax node)
    {
        var prev = _currentNamespace;
        _currentNamespace = node.NamespaceStatement.Name.ToString();
        base.VisitNamespaceBlock(node);
        _currentNamespace = prev;
    }

    public override void VisitClassBlock(ClassBlockSyntax node)
    {
        var prev = _currentClass;
        _currentClass = node.ClassStatement.Identifier.Text;
        base.VisitClassBlock(node);
        _currentClass = prev;
    }

    public override void VisitModuleBlock(ModuleBlockSyntax node)
    {
        var prev = _currentClass;
        _currentClass = node.ModuleStatement.Identifier.Text;
        base.VisitModuleBlock(node);
        _currentClass = prev;
    }

    public override void VisitMethodBlock(MethodBlockSyntax node)
    {
        var prev = _currentMethod;
        _currentMethod = node.SubOrFunctionStatement.Identifier.Text;
        base.VisitMethodBlock(node);
        _currentMethod = prev;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        TryExtractCall(node);
        base.VisitInvocationExpression(node);
    }

    private void TryExtractCall(InvocationExpressionSyntax node)
    {
        // Match patterns: provider.ExecuteReader(...), provider.ExecuteScalar(Of T)(...)
        string? methodName = null;
        string? genericTypeArg = null;

        switch (node.Expression)
        {
            // provider.ExecuteReader(...)
            case MemberAccessExpressionSyntax memberAccess:
                methodName = memberAccess.Name.Identifier.Text;
                if (memberAccess.Name is GenericNameSyntax genericName)
                {
                    methodName = genericName.Identifier.Text;
                    genericTypeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
                }
                break;

            // Nested: provider.ExecuteScalar(Of Integer)("...")
            // In VB, this can parse as InvocationExpression wrapping another InvocationExpression
            default:
                return;
        }

        if (methodName is null || !ExecuteMethods.Contains(methodName))
            return;

        var call = new DataProviderCall
        {
            MethodName = methodName,
            GenericTypeArgument = genericTypeArg,
            InvocationPattern = node.ToString().Replace("\r\n", " ").Replace("\n", " ").Trim(),
            ContainingMethod = _currentMethod,
            ContainingClass = _currentClass,
            LineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Extract arguments
        if (node.ArgumentList is not null)
        {
            var args = node.ArgumentList.Arguments;
            for (int i = 0; i < args.Count; i++)
            {
                var argText = args[i].GetExpression().ToString().Trim();

                if (i == 0 && argText.StartsWith("\"") && argText.EndsWith("\""))
                {
                    // First argument is procedure name string literal
                    call.ProcedureName = argText.Trim('"');
                }
                else
                {
                    call.Arguments.Add(argText);
                }
            }
        }

        _calls.Add(call);
    }
}
