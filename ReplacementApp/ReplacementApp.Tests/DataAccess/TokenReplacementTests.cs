using System.Data;
using ReplacementApp.Core.DataAccess;
using Xunit;

namespace ReplacementApp.Tests.DataAccess;

/// <summary>
/// {databaseOwner} / {objectQualifier} トークン置換のテスト。
/// </summary>
public class TokenReplacementTests : IDisposable
{
    private const string ConnectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True;" +
        "Persist Security Info=False;Encrypt=True;TrustServerCertificate=True";

    private readonly SqlDataProvider _provider;

    public TokenReplacementTests()
    {
        _provider = new SqlDataProvider(ConnectionString, "dbo.", "dnn_");
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Theory]
    [InlineData("{databaseOwner}{objectQualifier}ContentTypes", "dbo.dnn_ContentTypes")]
    [InlineData("{databaseOwner}SomeTable", "dbo.SomeTable")]
    [InlineData("{objectQualifier}Items", "dnn_Items")]
    [InlineData("NoTokensHere", "NoTokensHere")]
    [InlineData("SELECT * FROM {databaseOwner}{objectQualifier}Users WHERE Id = 1",
                "SELECT * FROM dbo.dnn_Users WHERE Id = 1")]
    public void ReplaceTokens_ReplacesCorrectly(string input, string expected)
    {
        var result = _provider.ReplaceTokens(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExecuteSQL_PerformsTokenReplacement()
    {
        // {databaseOwner}と{objectQualifier}がSQL実行時に置換される
        // sys.tablesは常に存在するのでトークン不要のクエリで接続確認
        using var reader = _provider.ExecuteSQL(
            "SELECT '{databaseOwner}' AS OwnerToken, '{objectQualifier}' AS QualifierToken");
        // 注: SQL文中のトークンは置換済みなので、結果は置換後の値
        Assert.NotNull(reader);
        Assert.True(reader.Read());
        Assert.Equal("dbo.", reader.GetString(0));
        Assert.Equal("dnn_", reader.GetString(1));
    }

    [Fact]
    public void ReplaceTokens_WithEmptyQualifier()
    {
        var provider = new SqlDataProvider(ConnectionString, "dbo.", "");
        var result = provider.ReplaceTokens("{databaseOwner}{objectQualifier}ContentTypes");
        Assert.Equal("dbo.ContentTypes", result);
        provider.Dispose();
    }
}
