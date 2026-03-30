namespace VBAnalyzer.Models;

public class EntityFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AccessModifier { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}
