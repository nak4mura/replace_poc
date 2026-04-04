using System.Data;
using ReplacementApp.Core.DataAccess;
using Xunit;

namespace ReplacementApp.Tests.DataAccess;

/// <summary>
/// SqlDataProviderの統合テスト。
/// localDB上のDotNetNukeデータベースに対して実行する。
/// </summary>
public class SqlDataProviderTests : IDisposable
{
    private const string ConnectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True;" +
        "Persist Security Info=False;Encrypt=True;TrustServerCertificate=True";

    private readonly SqlDataProvider _provider;

    public SqlDataProviderTests()
    {
        _provider = new SqlDataProvider(ConnectionString, "dbo.", "dnn_");
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        Assert.Equal("dbo.", _provider.DatabaseOwner);
        Assert.Equal("dnn_", _provider.ObjectQualifier);
        Assert.False(string.IsNullOrEmpty(_provider.ConnectionString));
    }

    [Fact]
    public void ResolveProcedureName_CombinesOwnerQualifierAndName()
    {
        var resolved = _provider.ResolveProcedureName("GetContentItem");
        Assert.Equal("dbo.dnn_GetContentItem", resolved);
    }

    [Fact]
    public void ExecuteSQL_CanConnectToDatabase()
    {
        // 基本的なSQL実行でDB接続が動作することを確認
        using var reader = _provider.ExecuteSQL("SELECT 1 AS TestValue");
        Assert.NotNull(reader);
        Assert.True(reader.Read());
        Assert.Equal(1, reader.GetInt32(0));
    }

    [Fact]
    public void ExecuteSQL_CanQuerySystemTables()
    {
        // システムテーブルへのクエリでDB接続を確認
        using var reader = _provider.ExecuteSQL("SELECT DB_NAME() AS DatabaseName");
        Assert.NotNull(reader);
        Assert.True(reader.Read());
        Assert.Equal("DotNetNuke", reader.GetString(0));
    }

    [Fact]
    public void ExecuteSQL_WithParameters()
    {
        var param = new Microsoft.Data.SqlClient.SqlParameter("@val", 42);
        using var reader = _provider.ExecuteSQL("SELECT @val AS Result", param);
        Assert.NotNull(reader);
        Assert.True(reader.Read());
        Assert.Equal(42, reader.GetInt32(0));
    }
}
