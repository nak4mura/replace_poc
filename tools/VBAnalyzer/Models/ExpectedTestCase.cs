namespace VBAnalyzer.Models;

public class ExpectedTestCase
{
    public string Id { get; set; } = string.Empty;
    public string? DbSetup { get; set; }
    public Dictionary<string, object?> Inputs { get; set; } = new();
    public ExpectedOutput? ExpectedOutput { get; set; }
    public List<ExpectedSqlCall> ExpectedSqlCalls { get; set; } = new();
}
