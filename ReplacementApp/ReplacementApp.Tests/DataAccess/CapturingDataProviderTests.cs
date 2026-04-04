using System.Data;
using System.Text.Json;
using ReplacementApp.Core.DataAccess;
using Xunit;

namespace ReplacementApp.Tests.DataAccess;

/// <summary>
/// CapturingDataProviderのテスト。
/// SQL呼び出しがキャプチャされ、JSON出力できることを検証する。
/// </summary>
public class CapturingDataProviderTests : IDisposable
{
    private const string ConnectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True;" +
        "Persist Security Info=False;Encrypt=True;TrustServerCertificate=True";

    private readonly SqlDataProvider _inner;

    public CapturingDataProviderTests()
    {
        _inner = new SqlDataProvider(ConnectionString, "dbo.", "dnn_");
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    [Fact]
    public void ExecuteSQL_CapturesCall()
    {
        var capturing = new CapturingDataProvider(_inner);

        using var reader = capturing.ExecuteSQL("SELECT 1 AS Val");

        Assert.Single(capturing.CapturedCalls);
        var call = capturing.CapturedCalls[0];
        Assert.Equal("ExecuteSQL", call.ExecuteMethod);
        Assert.Equal("SELECT 1 AS Val", call.ProcedureName);
    }

    [Fact]
    public void ExecuteSQL_WithParameters_CapturesParameters()
    {
        var capturing = new CapturingDataProvider(_inner);
        var param = new Microsoft.Data.SqlClient.SqlParameter("@val", 42);

        using var reader = capturing.ExecuteSQL("SELECT @val", param);

        Assert.Single(capturing.CapturedCalls);
        var call = capturing.CapturedCalls[0];
        Assert.Single(call.Parameters);
        Assert.Equal(42, call.Parameters[0].Value);
    }

    [Fact]
    public void MultipleCalls_CapturesAll()
    {
        var capturing = new CapturingDataProvider(_inner);

        using (var r1 = capturing.ExecuteSQL("SELECT 1"))
        { }
        using (var r2 = capturing.ExecuteSQL("SELECT 2"))
        { }

        Assert.Equal(2, capturing.CapturedCalls.Count);
    }

    [Fact]
    public void CapturedCalls_SerializeToJson()
    {
        var capturing = new CapturingDataProvider(_inner);

        using var reader = capturing.ExecuteSQL("SELECT 1 AS TestValue");

        var json = capturing.ToJson();
        Assert.False(string.IsNullOrWhiteSpace(json));

        // JSONとしてパースできることを確認
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);

        // 配列として1件のキャプチャが含まれる
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());

        var call = doc.RootElement[0];
        Assert.Equal("ExecuteSQL", call.GetProperty("executeMethod").GetString());
        Assert.Equal("SELECT 1 AS TestValue", call.GetProperty("procedureName").GetString());
    }

    [Fact]
    public void Clear_ResetsCapturedCalls()
    {
        var capturing = new CapturingDataProvider(_inner);

        using (var r = capturing.ExecuteSQL("SELECT 1"))
        { }
        Assert.Single(capturing.CapturedCalls);

        capturing.Clear();
        Assert.Empty(capturing.CapturedCalls);
    }

    [Fact]
    public void ToJson_ProducesValidCamelCaseJson()
    {
        var capturing = new CapturingDataProvider(_inner);
        var param = new Microsoft.Data.SqlClient.SqlParameter("@id", 5);

        using (var r = capturing.ExecuteSQL("SELECT @id", param))
        { }

        var json = capturing.ToJson();
        var doc = JsonDocument.Parse(json);
        var call = doc.RootElement[0];

        // camelCaseで出力されていることを確認
        Assert.True(call.TryGetProperty("executeMethod", out _));
        Assert.True(call.TryGetProperty("procedureName", out _));
        Assert.True(call.TryGetProperty("timestamp", out _));
        Assert.True(call.TryGetProperty("parameters", out _));

        // パラメータの中身も確認
        var parameters = call.GetProperty("parameters");
        Assert.Equal(1, parameters.GetArrayLength());
        Assert.Equal(0, parameters[0].GetProperty("index").GetInt32());
    }

    [Fact]
    public void DelegatesProperties_ToInnerProvider()
    {
        var capturing = new CapturingDataProvider(_inner);

        Assert.Equal(_inner.ConnectionString, capturing.ConnectionString);
        Assert.Equal(_inner.DatabaseOwner, capturing.DatabaseOwner);
        Assert.Equal(_inner.ObjectQualifier, capturing.ObjectQualifier);
    }
}
