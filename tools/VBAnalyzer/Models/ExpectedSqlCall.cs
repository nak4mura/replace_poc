namespace VBAnalyzer.Models;

public class ExpectedSqlCall
{
    public string ProcedureName { get; set; } = string.Empty;
    public List<ExpectedSqlParameter> Parameters { get; set; } = new();
}
