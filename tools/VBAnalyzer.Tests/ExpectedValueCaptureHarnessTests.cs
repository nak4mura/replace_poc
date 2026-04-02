using System.Data;
using VBAnalyzer.Capturing;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

[Collection("ComponentFactory")]
public class ExpectedValueCaptureHarnessTests : IDisposable
{
    public ExpectedValueCaptureHarnessTests()
    {
        ComponentFactory.Clear();
    }

    public void Dispose()
    {
        ComponentFactory.Clear();
    }

    [Fact]
    public void CaptureToExpectedConverter_ConvertCalls_ConvertsCorrectly()
    {
        var capturedCalls = new List<CapturedCall>
        {
            new()
            {
                ExecuteMethod = "ExecuteScalar<Int32>",
                ProcedureName = "dbo.dnn_AddContentItem",
                Parameters = new List<CapturedParameter>
                {
                    new() { Index = 0, Value = "Test", Type = "String" },
                    new() { Index = 1, Value = 1, Type = "Int32" }
                }
            }
        };

        var expectedCalls = CaptureToExpectedConverter.ConvertCalls(capturedCalls);

        Assert.Single(expectedCalls);
        Assert.Equal("dbo.dnn_AddContentItem", expectedCalls[0].ProcedureName);
        Assert.Equal(2, expectedCalls[0].Parameters.Count);
        Assert.Equal("Test", expectedCalls[0].Parameters[0].Value);
        Assert.Equal("String", expectedCalls[0].Parameters[0].Type);
    }

    [Fact]
    public void CaptureToExpectedConverter_ConvertOutput_IntValue()
    {
        var output = CaptureToExpectedConverter.ConvertOutput(42);

        Assert.Equal("Int32", output.Type);
        Assert.Equal(42, output.Value);
    }

    [Fact]
    public void CaptureToExpectedConverter_ConvertOutput_NullValue()
    {
        var output = CaptureToExpectedConverter.ConvertOutput(null);

        Assert.Equal("Null", output.Type);
        Assert.Null(output.Value);
    }

    [Fact]
    public void CaptureToExpectedConverter_ConvertOutput_DBNullValue()
    {
        var output = CaptureToExpectedConverter.ConvertOutput(DBNull.Value);

        Assert.Equal("Null", output.Type);
        Assert.Null(output.Value);
    }

    [Fact]
    public void CaptureToExpectedConverter_ConvertOutput_StringValue()
    {
        var output = CaptureToExpectedConverter.ConvertOutput("hello");

        Assert.Equal("String", output.Type);
        Assert.Equal("hello", output.Value);
    }

    [Fact]
    public void CaptureTestCase_WithFakeProvider_ReturnsExpectedTestCase()
    {
        var stub = new StubDataProvider();
        var sessionManager = new CaptureSessionManager();
        var capturingProvider = sessionManager.StartSession(stub, "test");

        // ハーネスを使わずに直接キャプチャフローをテスト
        ComponentFactory.RegisterComponentInstance<DataProvider>(capturingProvider);

        var provider = ComponentFactory.GetComponent<DataProvider>();
        var result = provider.ExecuteScalar<int>("AddContentItem", "Test Content", 1);

        var session = sessionManager.EndSession();

        var testCase = new ExpectedTestCase
        {
            Id = "test_case_1",
            Inputs = new Dictionary<string, object?> { { "content", "Test Content" }, { "contentTypeId", 1 } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(result),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturingProvider.Calls)
        };

        Assert.Equal("test_case_1", testCase.Id);
        Assert.Equal("Int32", testCase.ExpectedOutput.Type);
        Assert.Equal(42, testCase.ExpectedOutput.Value);
        Assert.Single(testCase.ExpectedSqlCalls);
        Assert.Equal("dbo.dnn_AddContentItem", testCase.ExpectedSqlCalls[0].ProcedureName);
        Assert.Equal(2, testCase.ExpectedSqlCalls[0].Parameters.Count);
    }

    [Fact]
    public void TestCaseSpec_CreatesSuiteWithMultipleCases()
    {
        var stub = new StubDataProvider();
        var suite = new ExpectedTestSuite { FunctionId = "ContentController.AddContentItem" };

        // ケース1
        var sessionManager1 = new CaptureSessionManager();
        var cap1 = sessionManager1.StartSession(stub, "case1");
        cap1.ExecuteScalar<int>("AddContentItem", "Content A", 1);
        sessionManager1.EndSession();
        suite.TestCases.Add(new ExpectedTestCase
        {
            Id = "case_a",
            Inputs = new Dictionary<string, object?> { { "content", "Content A" } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(42),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(cap1.Calls)
        });

        // ケース2
        var sessionManager2 = new CaptureSessionManager();
        var cap2 = sessionManager2.StartSession(stub, "case2");
        cap2.ExecuteScalar<int>("AddContentItem", "Content B", 2);
        sessionManager2.EndSession();
        suite.TestCases.Add(new ExpectedTestCase
        {
            Id = "case_b",
            Inputs = new Dictionary<string, object?> { { "content", "Content B" } },
            ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(42),
            ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(cap2.Calls)
        });

        Assert.Equal("ContentController.AddContentItem", suite.FunctionId);
        Assert.Equal(2, suite.TestCases.Count);
        Assert.Equal("case_a", suite.TestCases[0].Id);
        Assert.Equal("case_b", suite.TestCases[1].Id);
    }

    /// <summary>
    /// テスト用のスタブDataProvider。
    /// </summary>
    private class StubDataProvider : DataProvider
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
            return table.CreateDataReader();
        }

        public override void ExecuteNonQuery(string procedureName, params object[] commandParameters) { }
        public override object ExecuteScalar(string procedureName, params object[] commandParameters) => 42;
        public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
            => (T)Convert.ChangeType(42, typeof(T));
        public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters) => new();
        public override IDataReader ExecuteSQL(string sql) => new DataTable().CreateDataReader();
        public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
            => new DataTable().CreateDataReader();
        public override string ExecuteScript(string script) => string.Empty;
        public override string ExecuteScript(string script, bool useTransactions) => string.Empty;
    }
}
