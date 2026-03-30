using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBAnalyzer.Models;

namespace VBAnalyzer.Analyzers;

public class EntityExtractor : VisualBasicSyntaxWalker
{
    private readonly List<EntityInfo> _entities = new();
    private string? _currentNamespace;

    public IReadOnlyList<EntityInfo> Entities => _entities;

    public override void VisitNamespaceBlock(NamespaceBlockSyntax node)
    {
        var prev = _currentNamespace;
        _currentNamespace = node.NamespaceStatement.Name.ToString();
        base.VisitNamespaceBlock(node);
        _currentNamespace = prev;
    }

    public override void VisitClassBlock(ClassBlockSyntax node)
    {
        var entity = ExtractEntity(node);
        _entities.Add(entity);
        // Don't call base - we handle nested members directly
    }

    private EntityInfo ExtractEntity(ClassBlockSyntax node)
    {
        var classStatement = node.ClassStatement;
        var entity = new EntityInfo
        {
            ClassName = classStatement.Identifier.Text,
            Namespace = _currentNamespace,
            IsMustInherit = classStatement.Modifiers.Any(m => m.IsKind(SyntaxKind.MustInheritKeyword)),
            IsSerializable = HasAttribute(classStatement.AttributeLists, "Serializable")
        };

        // Inherits / Implements
        foreach (var inherits in node.Inherits)
        {
            foreach (var type in inherits.Types)
            {
                entity.BaseClass = type.ToString();
            }
        }

        foreach (var implements in node.Implements)
        {
            foreach (var type in implements.Types)
            {
                entity.ImplementedInterfaces.Add(type.ToString());
            }
        }

        // Fields and Properties
        foreach (var member in node.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    ExtractFields(field, entity);
                    break;
                case PropertyBlockSyntax property:
                    ExtractProperty(property.PropertyStatement, entity);
                    break;
                case PropertyStatementSyntax autoProperty:
                    ExtractProperty(autoProperty, entity);
                    break;
                case MethodBlockSyntax method:
                    TryExtractFillMethod(method, entity);
                    break;
            }
        }

        return entity;
    }

    private static void ExtractFields(FieldDeclarationSyntax field, EntityInfo entity)
    {
        var accessModifier = GetAccessModifier(field.Modifiers);
        foreach (var declarator in field.Declarators)
        {
            var type = declarator.AsClause is SimpleAsClauseSyntax asClause
                ? asClause.Type.ToString()
                : "Object";

            foreach (var name in declarator.Names)
            {
                var defaultValue = declarator.Initializer?.Value.ToString();
                entity.Fields.Add(new EntityFieldInfo
                {
                    Name = name.Identifier.Text,
                    Type = type,
                    AccessModifier = accessModifier,
                    DefaultValue = defaultValue
                });
            }
        }
    }

    private static void ExtractProperty(PropertyStatementSyntax prop, EntityInfo entity)
    {
        var type = prop.AsClause is SimpleAsClauseSyntax asClause
            ? asClause.Type.ToString()
            : "Object";

        var isReadOnly = prop.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

        entity.Properties.Add(new EntityPropertyInfo
        {
            Name = prop.Identifier.Text,
            Type = type,
            AccessModifier = GetAccessModifier(prop.Modifiers),
            IsReadOnly = isReadOnly,
            HasGetter = true,
            HasSetter = !isReadOnly
        });
    }

    private static void TryExtractFillMethod(MethodBlockSyntax method, EntityInfo entity)
    {
        var methodName = method.SubOrFunctionStatement.Identifier.Text;
        if (methodName is not ("Fill" or "FillInternal"))
            return;

        entity.HasFillMethod = true;
        entity.FillMethodName = methodName;

        // Extract column mappings from assignments like:
        // PropertyName = Null.SetNullInteger(dr("ColumnName"))
        foreach (var statement in method.Statements)
        {
            if (statement is AssignmentStatementSyntax assignment)
            {
                var propertyName = assignment.Left.ToString().Trim();
                var columnName = ExtractColumnName(assignment.Right);
                var nullType = ExtractNullType(assignment.Right);

                if (columnName is not null)
                {
                    var mapping = nullType is not null
                        ? $"{propertyName} <- {columnName} ({nullType})"
                        : $"{propertyName} <- {columnName}";
                    entity.FillFieldMappings.Add(mapping);
                }
            }
        }
    }

    private static string? ExtractColumnName(ExpressionSyntax expression)
    {
        // Pattern: Null.SetNull*(dr("ColumnName"))
        // or: dr("ColumnName")
        // or: Null.SetNull*(dr("ColumnName"))
        var text = expression.ToString();

        // Find dr("...") pattern
        var drIndex = text.IndexOf("dr(\"", StringComparison.Ordinal);
        if (drIndex < 0)
        {
            drIndex = text.IndexOf("dr(\"", StringComparison.OrdinalIgnoreCase);
        }

        if (drIndex >= 0)
        {
            var start = drIndex + 4; // skip dr("
            var end = text.IndexOf("\")", start, StringComparison.Ordinal);
            if (end > start)
            {
                return text.Substring(start, end - start);
            }
        }

        return null;
    }

    private static string? ExtractNullType(ExpressionSyntax expression)
    {
        var text = expression.ToString();

        if (text.Contains("SetNullInteger")) return "Integer";
        if (text.Contains("SetNullString")) return "String";
        if (text.Contains("SetNullBoolean")) return "Boolean";
        if (text.Contains("SetNullDateTime")) return "DateTime";
        if (text.Contains("SetNullDouble")) return "Double";
        if (text.Contains("SetNullSingle")) return "Single";
        if (text.Contains("SetNullShort")) return "Short";
        if (text.Contains("SetNullLong")) return "Long";

        return null;
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string name)
    {
        foreach (var list in attributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                if (attr.Name.ToString().Contains(name))
                    return true;
            }
        }
        return false;
    }

    private static string GetAccessModifier(SyntaxTokenList modifiers)
    {
        foreach (var modifier in modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword: return "Public";
                case SyntaxKind.PrivateKeyword: return "Private";
                case SyntaxKind.FriendKeyword: return "Friend";
                case SyntaxKind.ProtectedKeyword: return "Protected";
            }
        }
        return "Private"; // VB default for fields
    }
}
