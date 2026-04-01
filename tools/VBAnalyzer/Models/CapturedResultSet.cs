namespace VBAnalyzer.Models;

public class CapturedResultSet
{
    public List<string> ColumnNames { get; set; } = new();
    public List<List<object?>> Rows { get; set; } = new();
}
