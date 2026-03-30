namespace VBAnalyzer.Models;

public class DataProviderCall
{
    public string MethodName { get; set; } = string.Empty;
    public string? ProcedureName { get; set; }
    public string InvocationPattern { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public string? ContainingMethod { get; set; }
    public string? ContainingClass { get; set; }
    public int LineNumber { get; set; }
}
