using System.Data;
using System.Text.Json;
using VBAnalyzer.Capturing;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

/// <summary>
/// ComponentFactory経由でのCapturingDataProvider注入と、
/// DNN風コード呼び出しパターンの統合テスト。
/// </summary>
public class IntegrationTests : IDisposable
{
    public IntegrationTests()
    {
        ComponentFactory.Clear();
    }

    public void Dispose()
    {
        ComponentFactory.Clear();
    }

    [Fact]
    public void ComponentFactory_RegisterAndResolve_DataProvider()
    {
        var stub = new FakeDataProvider();
        var capturing = new CapturingDataProvider(stub);

        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var resolved = ComponentFactory.GetComponent<DataProvider>();
        Assert.Same(capturing, resolved);
    }

    [Fact]
    public void ComponentFactory_TransparentInjection_ExistingCodeUnchanged()
    {
        var stub = new FakeDataProvider();
        var capturing = new CapturingDataProvider(stub);
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        // DNN風のコード：ComponentFactory経由でDataProviderを取得して使用
        var provider = ComponentFactory.GetComponent<DataProvider>();
        provider.ExecuteNonQuery("AddContentItem", "Test Content", 1, -1);

        // キャプチャされていることを確認
        var capturingProvider = (CapturingDataProvider)provider;
        Assert.Single(capturingProvider.Calls);
        Assert.Equal("dbo.dnn_AddContentItem", capturingProvider.Calls[0].ProcedureName);
    }

    [Fact]
    public void Integration_AddContentItem_CapturesCorrectSqlCall()
    {
        var manager = new CaptureSessionManager();
        var stub = new FakeDataProvider();
        var capturing = manager.StartSession(stub, "integration-test-1");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        // DNN DataService.AddContentItem相当の呼び出しをシミュレート
        var provider = ComponentFactory.GetComponent<DataProvider>();
        var contentItemId = provider.ExecuteScalar<int>("AddContentItem",
            "Test Content",   // Content
            1,                // ContentTypeId
            -1,               // TabId
            -1,               // ModuleId
            "Test Title",     // ContentTitle
            "",               // ContentKey
            true              // Indexed
        );

        var session = manager.EndSession();

        Assert.Equal("integration-test-1", session.SessionId);
        Assert.Single(session.Calls);

        var call = session.Calls[0];
        Assert.Equal("ExecuteScalar<Int32>", call.ExecuteMethod);
        Assert.Equal("dbo.dnn_AddContentItem", call.ProcedureName);
        Assert.Equal(7, call.Parameters.Count);
        Assert.Equal("Test Content", call.Parameters[0].Value);
        Assert.Equal("String", call.Parameters[0].Type);
        Assert.Equal(1, call.Parameters[1].Value);
        Assert.Equal("Int32", call.Parameters[1].Type);
        Assert.Equal(42, call.ReturnValue);
    }

    [Fact]
    public void Integration_ExecuteReader_ReaderConsumedNormally()
    {
        var manager = new CaptureSessionManager();
        var stub = new FakeDataProvider();
        var capturing = manager.StartSession(stub, "reader-test");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // DNN CBO.FillCollection相当のパターンをシミュレート
        var items = new List<Dictionary<string, object?>>();
        using (var reader = provider.ExecuteReader("GetContentItems", 1))
        {
            while (reader.Read())
            {
                var item = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                items.Add(item);
            }
        }

        // 実データが正しく読めていること
        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0]["ContentItemId"]);
        Assert.Equal("Item1", items[0]["Content"]);

        // キャプチャも正しく記録されていること
        var session = manager.EndSession();
        var call = session.Calls[0];
        Assert.Equal("ExecuteReader", call.ExecuteMethod);
        Assert.Equal(2, call.ResultSets[0].Rows.Count);
        Assert.Equal(new[] { "ContentItemId", "Content" }, call.ResultSets[0].ColumnNames);
    }

    [Fact]
    public void Integration_MultipleCallsSession_JsonOutput()
    {
        var manager = new CaptureSessionManager();
        var stub = new FakeDataProvider();
        var capturing = manager.StartSession(stub, "multi-call-test");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // 複数の呼び出し
        using (var reader = provider.ExecuteReader("GetContentItems", 1))
        {
            while (reader.Read()) { }
        }
        provider.ExecuteNonQuery("UpdateContentItem", 1, "Updated Content");
        var count = provider.ExecuteScalar<int>("CountContentItems");

        var session = manager.EndSession();
        var json = CaptureSessionManager.ToJson(session);

        // JSON出力の検証
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("multi-call-test", root.GetProperty("sessionId").GetString());

        var calls = root.GetProperty("calls");
        Assert.Equal(3, calls.GetArrayLength());

        Assert.Equal("ExecuteReader", calls[0].GetProperty("executeMethod").GetString());
        Assert.Equal("ExecuteNonQuery", calls[1].GetProperty("executeMethod").GetString());
        Assert.Equal("ExecuteScalar<Int32>", calls[2].GetProperty("executeMethod").GetString());
    }

    [Fact]
    public void Integration_SessionSaveAndLoad_RoundTrip()
    {
        var manager = new CaptureSessionManager();
        var stub = new FakeDataProvider();
        var capturing = manager.StartSession(stub, "file-roundtrip");

        capturing.ExecuteNonQuery("TestProc", 1);
        capturing.ExecuteScalar<int>("CountProc");

        var session = manager.EndSession();

        var tempFile = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.json");
        try
        {
            CaptureSessionManager.SaveToFile(session, tempFile);
            var loaded = CaptureSessionManager.LoadFromFile(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal("file-roundtrip", loaded.SessionId);
            Assert.Equal(2, loaded.Calls.Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    /// <summary>
    /// DNN DataProviderのスタブ実装。
    /// 実際のDBアクセスの代わりにインメモリDataTableを返す。
    /// </summary>
    private class FakeDataProvider : DataProvider
    {
        public override string ConnectionString => "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke";
        public override string DatabaseOwner => "dbo.";
        public override string ObjectQualifier => "dnn_";

        public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
        {
            var table = new DataTable();
            table.Columns.Add("ContentItemId", typeof(int));
            table.Columns.Add("Content", typeof(string));
            table.Rows.Add(1, "Item1");
            table.Rows.Add(2, "Item2");
            return table.CreateDataReader();
        }

        public override void ExecuteNonQuery(string procedureName, params object[] commandParameters) { }

        public override object ExecuteScalar(string procedureName, params object[] commandParameters) => 42;

        public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
        {
            var result = ExecuteScalar(procedureName, commandParameters);
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters) => new();

        public override IDataReader ExecuteSQL(string sql) => new DataTable().CreateDataReader();

        public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
            => new DataTable().CreateDataReader();

        public override string ExecuteScript(string script) => string.Empty;
        public override string ExecuteScript(string script, bool useTransactions) => string.Empty;
    }
}
