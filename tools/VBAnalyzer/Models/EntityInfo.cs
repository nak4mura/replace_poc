namespace VBAnalyzer.Models;

public class EntityInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? BaseClass { get; set; }
    public List<string> ImplementedInterfaces { get; set; } = new();
    public bool IsSerializable { get; set; }
    public bool IsMustInherit { get; set; }
    public List<EntityFieldInfo> Fields { get; set; } = new();
    public List<EntityPropertyInfo> Properties { get; set; } = new();
    public bool HasFillMethod { get; set; }
    public string? FillMethodName { get; set; }
    public List<string> FillFieldMappings { get; set; } = new();
}
