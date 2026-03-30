using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBAnalyzer.Models;

namespace VBAnalyzer.Analyzers;

public class MethodSignatureExtractor : VisualBasicSyntaxWalker
{
    private readonly List<MethodSignature> _methods = new();
    private string? _currentNamespace;
    private string? _currentClass;

    public IReadOnlyList<MethodSignature> Methods => _methods;

    public override void VisitNamespaceBlock(NamespaceBlockSyntax node)
    {
        var previousNamespace = _currentNamespace;
        _currentNamespace = node.NamespaceStatement.Name.ToString();
        base.VisitNamespaceBlock(node);
        _currentNamespace = previousNamespace;
    }

    public override void VisitClassBlock(ClassBlockSyntax node)
    {
        var previousClass = _currentClass;
        _currentClass = node.ClassStatement.Identifier.Text;
        base.VisitClassBlock(node);
        _currentClass = previousClass;
    }

    public override void VisitModuleBlock(ModuleBlockSyntax node)
    {
        var previousClass = _currentClass;
        _currentClass = node.ModuleStatement.Identifier.Text;
        base.VisitModuleBlock(node);
        _currentClass = previousClass;
    }

    public override void VisitMethodStatement(MethodStatementSyntax node)
    {
        var method = ExtractMethod(node);
        _methods.Add(method);
        base.VisitMethodStatement(node);
    }

    private MethodSignature ExtractMethod(MethodStatementSyntax node)
    {
        var method = new MethodSignature
        {
            Name = node.Identifier.Text,
            ContainingClass = _currentClass,
            ContainingNamespace = _currentNamespace,
            LineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };

        // Sub vs Function
        method.MethodKind = node.SubOrFunctionKeyword.IsKind(SyntaxKind.FunctionKeyword)
            ? "Function"
            : "Sub";

        // Return type (Function only)
        if (node.AsClause is SimpleAsClauseSyntax asClause)
        {
            method.ReturnType = asClause.Type.ToString();
        }

        // Access modifier
        method.AccessModifier = GetAccessModifier(node.Modifiers);

        // Other modifiers
        foreach (var modifier in node.Modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.SharedKeyword:
                    method.IsShared = true;
                    break;
                case SyntaxKind.MustOverrideKeyword:
                    method.IsMustOverride = true;
                    break;
                case SyntaxKind.OverridableKeyword:
                    method.IsOverridable = true;
                    break;
                case SyntaxKind.OverridesKeyword:
                    method.IsOverrides = true;
                    break;
                case SyntaxKind.OverloadsKeyword:
                    method.IsOverloads = true;
                    break;
            }
        }

        // Generic type parameters
        if (node.TypeParameterList is not null)
        {
            foreach (var typeParam in node.TypeParameterList.Parameters)
            {
                method.GenericTypeParameters.Add(typeParam.Identifier.Text);
            }
        }

        // Parameters
        if (node.ParameterList is not null)
        {
            int ordinal = 0;
            foreach (var param in node.ParameterList.Parameters)
            {
                method.Parameters.Add(ExtractParameter(param, ordinal++));
            }
        }

        return method;
    }

    private static Models.ParameterInfo ExtractParameter(ParameterSyntax param, int ordinal)
    {
        var info = new Models.ParameterInfo
        {
            Name = param.Identifier.Identifier.Text,
            Ordinal = ordinal
        };

        // Type
        if (param.AsClause is SimpleAsClauseSyntax asClause)
        {
            info.Type = asClause.Type.ToString();
        }

        // Modifiers: ByRef, ParamArray, Optional
        foreach (var modifier in param.Modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.ByRefKeyword:
                    info.IsByRef = true;
                    break;
                case SyntaxKind.ParamArrayKeyword:
                    info.IsParamArray = true;
                    break;
                case SyntaxKind.OptionalKeyword:
                    info.IsOptional = true;
                    break;
            }
        }

        // Default value for Optional parameters
        if (param.Default is not null)
        {
            info.DefaultValue = param.Default.Value.ToString();
        }

        return info;
    }

    private static string GetAccessModifier(SyntaxTokenList modifiers)
    {
        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    return "Public";
                case SyntaxKind.PrivateKeyword:
                    return "Private";
                case SyntaxKind.FriendKeyword:
                    return "Friend";
                case SyntaxKind.ProtectedKeyword:
                    return "Protected";
            }
        }
        return "Public"; // VB default
    }
}
