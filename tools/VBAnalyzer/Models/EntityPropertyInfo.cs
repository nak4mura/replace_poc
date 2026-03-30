namespace VBAnalyzer.Models;

public class EntityPropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AccessModifier { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }
}
