namespace ReplacementApp.Tests.Verification.Models;

public class ExpectedSqlCall
{
    public string ProcedureName { get; set; } = string.Empty;
    public List<ExpectedSqlParameter> Parameters { get; set; } = new();
}
