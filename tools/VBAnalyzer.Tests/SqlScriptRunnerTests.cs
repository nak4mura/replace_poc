using VBAnalyzer.TestInfrastructure;
using Xunit;

namespace VBAnalyzer.Tests;

public class SqlScriptRunnerTests
{
    [Fact]
    public void ReplaceTokens_ReplacesDatabaseOwner()
    {
        var runner = new SqlScriptRunner("dbo.", "dnn_");
        var result = runner.ReplaceTokens("SELECT * FROM {databaseOwner}[{objectQualifier}ContentItems]");
        Assert.Equal("SELECT * FROM dbo.[dnn_ContentItems]", result);
    }

    [Fact]
    public void ReplaceTokens_ReplacesObjectQualifier()
    {
        var runner = new SqlScriptRunner("dbo.", "test_");
        var result = runner.ReplaceTokens("{databaseOwner}[{objectQualifier}MyTable]");
        Assert.Equal("dbo.[test_MyTable]", result);
    }

    [Fact]
    public void ReplaceTokens_EmptyQualifier_ProducesCleanOutput()
    {
        var runner = new SqlScriptRunner("dbo.", "");
        var result = runner.ReplaceTokens("{databaseOwner}[{objectQualifier}ContentItems]");
        Assert.Equal("dbo.[ContentItems]", result);
    }

    [Fact]
    public void ReplaceTokens_NoTokens_ReturnsOriginal()
    {
        var runner = new SqlScriptRunner();
        var result = runner.ReplaceTokens("SELECT 1");
        Assert.Equal("SELECT 1", result);
    }
}
