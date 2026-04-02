using System.Data;
using System.Text.Json;
using VBAnalyzer.Capturing;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

/// <summary>
/// ContentControllerの対象関数（AddContentItem, GetContentItem, DeleteContentItem）の
/// 期待値テストデータ生成テスト。
/// DNN DataServiceの呼び出しパターンをシミュレートし、期待値JSONを生成する。
/// </summary>
[Collection("ComponentFactory")]
public class ContentControllerExpectedValueTests : IDisposable
{
    private readonly string _outputDir;
    private readonly ExpectedValueWriter _writer;

    public ContentControllerExpectedValueTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"expected_values_{Guid.NewGuid()}");
        _writer = new ExpectedValueWriter(_outputDir);
        ComponentFactory.Clear();
    }

    public void Dispose()
    {
        ComponentFactory.Clear();
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, true);
    }

    #region AddContentItem

    [Fact]
    public void AddContentItem_ValidInput_CapturesExpectedValues()
    {
        var stub = new ContentStubDataProvider();
        var sessionManager = new CaptureSessionManager();
        var capturing = sessionManager.StartSession(stub, "add-content-valid");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // DataService.AddContentItem の呼び出しをシミュレート
        // provider.ExecuteScalar(Of Integer)("AddContentItem", content, contentTypeId, tabId, moduleId, contentKey, indexed, createdByUserId)
        var result = provider.ExecuteScalar<int>("AddContentItem",
            "Content",    // content
            1,            // contentTypeId
            10,           // tabId
            30,           // moduleId
            "ContentKey", // contentKey
            true,         // indexed
            1             // createdByUserId
        );

        var session = sessionManager.EndSession();

        var testCase = new ExpectedTestCase
        {
            Id = "valid_content_item",
            DbSetup = "scripts/content_setup.sql",
            Inputs = new Dictionary<string, object?>
            {
                { "content", "Content" },
                { "contentTypeId", 1 },
                { "tabId", 10 },
                { "moduleId", 30 },
                { "contentKey", "ContentKey" },
                { "indexed", true },
                { "createdByUserId", 1 }
            },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(result),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturing.Calls)
        };

        // 検証: 戻り値
        Assert.Equal("Int32", testCase.ExpectedOutput!.Type);

        // 検証: SQL呼び出し
        Assert.Single(testCase.ExpectedSqlCalls);
        var sqlCall = testCase.ExpectedSqlCalls[0];
        Assert.Equal("dbo.dnn_AddContentItem", sqlCall.ProcedureName);
        Assert.Equal(7, sqlCall.Parameters.Count);
        Assert.Equal("Content", sqlCall.Parameters[0].Value);
        Assert.Equal("String", sqlCall.Parameters[0].Type);
        Assert.Equal(1, sqlCall.Parameters[1].Value);
        Assert.Equal("Int32", sqlCall.Parameters[1].Type);

        // JSON出力
        var suite = new ExpectedTestSuite
        {
            FunctionId = "ContentController.AddContentItem",
            TestCases = new List<ExpectedTestCase> { testCase }
        };
        _writer.Write(suite);

        // ファイルの存在と内容確認
        var filePath = _writer.ResolveOutputPath("ContentController.AddContentItem");
        Assert.True(File.Exists(filePath));

        var loaded = ExpectedValueWriter.Load(filePath);
        Assert.NotNull(loaded);
        Assert.Equal("ContentController.AddContentItem", loaded.FunctionId);
        Assert.Single(loaded.TestCases);
    }

    #endregion

    #region GetContentItem

    [Fact]
    public void GetContentItem_ValidId_CapturesExpectedValues()
    {
        var stub = new ContentStubDataProvider();
        var sessionManager = new CaptureSessionManager();
        var capturing = sessionManager.StartSession(stub, "get-content-valid");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // DataService.GetContentItem の呼び出しをシミュレート
        // provider.ExecuteReader("GetContentItem", contentItemId)
        using var reader = provider.ExecuteReader("GetContentItem", 1);

        // CBO.FillObject相当: IDataReaderからエンティティを構築
        Dictionary<string, object?>? contentItem = null;
        if (reader.Read())
        {
            contentItem = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                contentItem[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
        }

        var session = sessionManager.EndSession();

        var testCase = new ExpectedTestCase
        {
            Id = "valid_content_item_id",
            DbSetup = "scripts/content_setup.sql",
            Inputs = new Dictionary<string, object?> { { "contentItemId", 1 } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(contentItem),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturing.Calls)
        };

        // 検証: SQL呼び出し
        Assert.Single(testCase.ExpectedSqlCalls);
        var sqlCall = testCase.ExpectedSqlCalls[0];
        Assert.Equal("dbo.dnn_GetContentItem", sqlCall.ProcedureName);
        Assert.Single(sqlCall.Parameters);
        Assert.Equal(1, sqlCall.Parameters[0].Value);
        Assert.Equal("Int32", sqlCall.Parameters[0].Type);

        // 検証: 戻り値（ContentItemのフィールド）
        Assert.NotNull(contentItem);
        Assert.Equal(1, contentItem!["ContentItemId"]);
        Assert.Equal("Content 1", contentItem["Content"]);

        // JSON出力
        _writer.AppendTestCase("ContentController.GetContentItem", testCase);
        var filePath = _writer.ResolveOutputPath("ContentController.GetContentItem");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void GetContentItem_InvalidId_CapturesExpectedValues()
    {
        var stub = new ContentStubDataProvider();
        var sessionManager = new CaptureSessionManager();
        var capturing = sessionManager.StartSession(stub, "get-content-invalid");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // 存在しないIDでGetContentItemを呼び出し
        using var reader = provider.ExecuteReader("GetContentItem", 999);

        // CBO.FillObjectはReaderが空の場合nullを返す
        Dictionary<string, object?>? contentItem = null;
        if (reader.Read())
        {
            contentItem = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                contentItem[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
        }

        var session = sessionManager.EndSession();

        var testCase = new ExpectedTestCase
        {
            Id = "invalid_content_item_id",
            DbSetup = "scripts/content_setup.sql",
            Inputs = new Dictionary<string, object?> { { "contentItemId", 999 } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(contentItem),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturing.Calls)
        };

        // 検証: SQL呼び出しは発行される（データが無いだけ）
        Assert.Single(testCase.ExpectedSqlCalls);
        Assert.Equal("dbo.dnn_GetContentItem", testCase.ExpectedSqlCalls[0].ProcedureName);
        Assert.Equal(999, testCase.ExpectedSqlCalls[0].Parameters[0].Value);

        // JSON出力
        _writer.AppendTestCase("ContentController.GetContentItem", testCase);
    }

    #endregion

    #region DeleteContentItem

    [Fact]
    public void DeleteContentItem_ValidItem_CapturesExpectedValues()
    {
        var stub = new ContentStubDataProvider();
        var sessionManager = new CaptureSessionManager();
        var capturing = sessionManager.StartSession(stub, "delete-content-valid");
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturing);

        var provider = ComponentFactory.GetComponent<DataProvider>();

        // DataService.DeleteContentItem の呼び出しをシミュレート
        // provider.ExecuteNonQuery("DeleteContentItem", contentItem.ContentItemId)
        provider.ExecuteNonQuery("DeleteContentItem", 3);

        var session = sessionManager.EndSession();

        var testCase = new ExpectedTestCase
        {
            Id = "valid_delete",
            DbSetup = "scripts/content_setup.sql",
            Inputs = new Dictionary<string, object?> { { "contentItemId", 3 } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(null),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturing.Calls)
        };

        // 検証: SQL呼び出し
        Assert.Single(testCase.ExpectedSqlCalls);
        var sqlCall = testCase.ExpectedSqlCalls[0];
        Assert.Equal("dbo.dnn_DeleteContentItem", sqlCall.ProcedureName);
        Assert.Single(sqlCall.Parameters);
        Assert.Equal(3, sqlCall.Parameters[0].Value);
        Assert.Equal("Int32", sqlCall.Parameters[0].Type);

        // 検証: 戻り値はNull（void）
        Assert.Equal("Null", testCase.ExpectedOutput!.Type);

        // JSON出力
        _writer.AppendTestCase("ContentController.DeleteContentItem", testCase);
        var filePath = _writer.ResolveOutputPath("ContentController.DeleteContentItem");
        Assert.True(File.Exists(filePath));
    }

    #endregion

    #region Full Suite Generation

    [Fact]
    public void GenerateFullSuite_AllFunctions_OutputsValidJson()
    {
        var stub = new ContentStubDataProvider();

        // AddContentItem
        var addSession = new CaptureSessionManager();
        var addCap = addSession.StartSession(stub, "add");
        var addResult = addCap.ExecuteScalar<int>("AddContentItem", "Content", 1, 10, 30, "ContentKey", true, 1);
        addSession.EndSession();

        var addSuite = new ExpectedTestSuite
        {
            FunctionId = "ContentController.AddContentItem",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "valid_content_item",
                    DbSetup = "scripts/content_setup.sql",
                    Inputs = new Dictionary<string, object?>
                    {
                        { "content", "Content" }, { "contentTypeId", 1 }, { "tabId", 10 },
                        { "moduleId", 30 }, { "contentKey", "ContentKey" }, { "indexed", true },
                        { "createdByUserId", 1 }
                    },
                    ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(addResult),
                    ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(addCap.Calls)
                }
            }
        };
        _writer.Write(addSuite);

        // GetContentItem
        var getSession = new CaptureSessionManager();
        var getCap = getSession.StartSession(stub, "get");
        using (var reader = getCap.ExecuteReader("GetContentItem", 1)) { while (reader.Read()) { } }
        getSession.EndSession();

        var getSuite = new ExpectedTestSuite
        {
            FunctionId = "ContentController.GetContentItem",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "valid_id",
                    DbSetup = "scripts/content_setup.sql",
                    Inputs = new Dictionary<string, object?> { { "contentItemId", 1 } },
                    ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(getCap.Calls)
                }
            }
        };
        _writer.Write(getSuite);

        // DeleteContentItem
        var delSession = new CaptureSessionManager();
        var delCap = delSession.StartSession(stub, "del");
        delCap.ExecuteNonQuery("DeleteContentItem", 3);
        delSession.EndSession();

        var delSuite = new ExpectedTestSuite
        {
            FunctionId = "ContentController.DeleteContentItem",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "valid_delete",
                    DbSetup = "scripts/content_setup.sql",
                    Inputs = new Dictionary<string, object?> { { "contentItemId", 3 } },
                    ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(null),
                    ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(delCap.Calls)
                }
            }
        };
        _writer.Write(delSuite);

        // 全ファイルの存在確認
        Assert.True(File.Exists(_writer.ResolveOutputPath("ContentController.AddContentItem")));
        Assert.True(File.Exists(_writer.ResolveOutputPath("ContentController.GetContentItem")));
        Assert.True(File.Exists(_writer.ResolveOutputPath("ContentController.DeleteContentItem")));

        // JSON構造の検証
        var addLoaded = ExpectedValueWriter.Load(_writer.ResolveOutputPath("ContentController.AddContentItem"))!;
        Assert.Equal("ContentController.AddContentItem", addLoaded.FunctionId);
        Assert.Single(addLoaded.TestCases);
        Assert.Equal("dbo.dnn_AddContentItem", addLoaded.TestCases[0].ExpectedSqlCalls[0].ProcedureName);
        Assert.Equal(7, addLoaded.TestCases[0].ExpectedSqlCalls[0].Parameters.Count);

        var getLoaded = ExpectedValueWriter.Load(_writer.ResolveOutputPath("ContentController.GetContentItem"))!;
        Assert.Equal("dbo.dnn_GetContentItem", getLoaded.TestCases[0].ExpectedSqlCalls[0].ProcedureName);

        var delLoaded = ExpectedValueWriter.Load(_writer.ResolveOutputPath("ContentController.DeleteContentItem"))!;
        Assert.Equal("dbo.dnn_DeleteContentItem", delLoaded.TestCases[0].ExpectedSqlCalls[0].ProcedureName);
    }

    #endregion

    /// <summary>
    /// ContentController テスト用のスタブDataProvider。
    /// DNN ContentDataServiceの呼び出しパターンに対応する。
    /// </summary>
    private class ContentStubDataProvider : DataProvider
    {
        public override string ConnectionString => "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke";
        public override string DatabaseOwner => "dbo.";
        public override string ObjectQualifier => "dnn_";

        public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
        {
            var table = new DataTable();
            table.Columns.Add("ContentItemId", typeof(int));
            table.Columns.Add("Content", typeof(string));
            table.Columns.Add("ContentTypeId", typeof(int));
            table.Columns.Add("TabID", typeof(int));
            table.Columns.Add("ModuleID", typeof(int));
            table.Columns.Add("ContentKey", typeof(string));
            table.Columns.Add("Indexed", typeof(bool));
            table.Columns.Add("CreatedByUserID", typeof(int));
            table.Columns.Add("CreatedOnDate", typeof(DateTime));
            table.Columns.Add("LastModifiedByUserID", typeof(int));
            table.Columns.Add("LastModifiedOnDate", typeof(DateTime));

            // contentItemIdが999（無効ID）の場合は空のReaderを返す
            if (commandParameters.Length > 0 && commandParameters[0] is int id && id == 999)
            {
                return table.CreateDataReader();
            }

            // テストデータ: ContentItem ID=1
            table.Rows.Add(1, "Content 1", 1, 10, 30, "ContentKey 1", true, 1,
                new DateTime(2026, 1, 1), 1, new DateTime(2026, 1, 1));
            return table.CreateDataReader();
        }

        public override void ExecuteNonQuery(string procedureName, params object[] commandParameters) { }

        public override object ExecuteScalar(string procedureName, params object[] commandParameters) => 2;

        public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
            => (T)Convert.ChangeType(2, typeof(T));

        public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters) => new();

        public override IDataReader ExecuteSQL(string sql) => new DataTable().CreateDataReader();

        public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
            => new DataTable().CreateDataReader();

        public override string ExecuteScript(string script) => string.Empty;
        public override string ExecuteScript(string script, bool useTransactions) => string.Empty;
    }
}
