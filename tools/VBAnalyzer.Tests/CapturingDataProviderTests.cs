using System.Data;
using VBAnalyzer.Capturing;
using Xunit;

namespace VBAnalyzer.Tests;

public class CapturingDataProviderTests
{
    [Fact]
    public void ExecuteReader_CapturesCallAndWrapsReader()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        using var reader = provider.ExecuteReader("GetUser", 1, "admin");

        Assert.Single(provider.Calls);
        var call = provider.Calls[0];
        Assert.Equal("ExecuteReader", call.ExecuteMethod);
        Assert.Equal("dbo.dnn_GetUser", call.ProcedureName);
        Assert.Equal(2, call.Parameters.Count);
        Assert.Equal(1, call.Parameters[0].Value);
        Assert.Equal("Int32", call.Parameters[0].Type);
        Assert.Equal("admin", call.Parameters[1].Value);

        // Reader should still work
        Assert.True(reader.Read());
        Assert.Equal(42, reader.GetInt32(0));
    }

    [Fact]
    public void ExecuteNonQuery_CapturesCall()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        provider.ExecuteNonQuery("DeleteUser", 1);

        Assert.Single(provider.Calls);
        var call = provider.Calls[0];
        Assert.Equal("ExecuteNonQuery", call.ExecuteMethod);
        Assert.Equal("dbo.dnn_DeleteUser", call.ProcedureName);
        Assert.Single(call.Parameters);
    }

    [Fact]
    public void ExecuteScalar_CapturesCallAndReturnValue()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        var result = provider.ExecuteScalar("CountUsers");

        Assert.Equal(99, result);
        Assert.Single(provider.Calls);
        var call = provider.Calls[0];
        Assert.Equal("ExecuteScalar", call.ExecuteMethod);
        Assert.Equal(99, call.ReturnValue);
    }

    [Fact]
    public void ExecuteScalarGeneric_CapturesCallWithTypeName()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        var result = provider.ExecuteScalar<int>("CountUsers");

        Assert.Equal(99, result);
        Assert.Single(provider.Calls);
        Assert.Equal("ExecuteScalar<Int32>", provider.Calls[0].ExecuteMethod);
    }

    [Fact]
    public void ExecuteSQL_CapturesCall()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        using var reader = provider.ExecuteSQL("SELECT 1");

        Assert.Single(provider.Calls);
        Assert.Equal("ExecuteSQL", provider.Calls[0].ExecuteMethod);
        Assert.Equal("SELECT 1", provider.Calls[0].ProcedureName);
    }

    [Fact]
    public void MultipleCalls_AllCaptured()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        provider.ExecuteNonQuery("Proc1");
        provider.ExecuteNonQuery("Proc2");
        provider.ExecuteScalar("Proc3");

        Assert.Equal(3, provider.Calls.Count);
    }

    [Fact]
    public void ExecuteReader_ResultSetsLinkedToCapturedCall()
    {
        var stub = new StubDataProvider();
        var provider = new CapturingDataProvider(stub);

        using var reader = provider.ExecuteReader("GetUser", 1);

        // Read through the data to trigger capture
        while (reader.Read()) { }

        var call = provider.Calls[0];
        Assert.Single(call.ResultSets);
        Assert.Single(call.ResultSets[0].Rows);
        Assert.Equal(42, call.ResultSets[0].Rows[0][0]);
    }

    /// <summary>
    /// テスト用のスタブDataProvider実装。
    /// </summary>
    private class StubDataProvider : DataProvider
    {
        public override string ConnectionString => "test-connection";
        public override string DatabaseOwner => "dbo.";
        public override string ObjectQualifier => "dnn_";

        public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(42);
            return table.CreateDataReader();
        }

        public override void ExecuteNonQuery(string procedureName, params object[] commandParameters) { }

        public override object ExecuteScalar(string procedureName, params object[] commandParameters) => 99;

        public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
        {
            var result = ExecuteScalar(procedureName, commandParameters);
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters) => new();

        public override IDataReader ExecuteSQL(string sql)
        {
            var table = new DataTable();
            return table.CreateDataReader();
        }

        public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
        {
            var table = new DataTable();
            return table.CreateDataReader();
        }

        public override string ExecuteScript(string script) => string.Empty;
        public override string ExecuteScript(string script, bool useTransactions) => string.Empty;
    }
}
