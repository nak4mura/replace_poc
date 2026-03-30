namespace VBAnalyzer.Models;

public class MethodSignature
{
    public string Name { get; set; } = string.Empty;
    public string? ReturnType { get; set; }
    public string AccessModifier { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public bool IsMustOverride { get; set; }
    public bool IsOverridable { get; set; }
    public bool IsOverrides { get; set; }
    public bool IsOverloads { get; set; }
    public string MethodKind { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
    public List<string> GenericTypeParameters { get; set; } = new();
    public string? ContainingClass { get; set; }
    public string? ContainingNamespace { get; set; }
    public int LineNumber { get; set; }
}
