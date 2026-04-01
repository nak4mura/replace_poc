namespace VBAnalyzer.Models;

public class CapturedCall
{
    public DateTime Timestamp { get; set; }
    public string ExecuteMethod { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public List<CapturedParameter> Parameters { get; set; } = new();
    public object? ReturnValue { get; set; }
    public List<CapturedResultSet> ResultSets { get; set; } = new();
}
