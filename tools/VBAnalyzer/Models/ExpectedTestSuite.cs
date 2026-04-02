namespace VBAnalyzer.Models;

public class ExpectedTestSuite
{
    public string FunctionId { get; set; } = string.Empty;
    public List<ExpectedTestCase> TestCases { get; set; } = new();
}
