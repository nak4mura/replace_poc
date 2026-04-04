namespace ReplacementApp.Tests.Verification.Models;

public class ExpectedSqlParameter
{
    public int Index { get; set; }
    public object? Value { get; set; }
    public string Type { get; set; } = string.Empty;
}
