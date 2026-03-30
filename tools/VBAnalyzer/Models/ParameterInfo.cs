namespace VBAnalyzer.Models;

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsByRef { get; set; }
    public bool IsParamArray { get; set; }
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
    public int Ordinal { get; set; }
}
