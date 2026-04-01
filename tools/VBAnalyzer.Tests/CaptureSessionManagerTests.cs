using System.Data;
using System.Text.Json;
using VBAnalyzer.Capturing;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

public class CaptureSessionManagerTests
{
    [Fact]
    public void StartSession_CreatesNewSession()
    {
        var manager = new CaptureSessionManager();
        var stub = new StubDataProvider();

        var provider = manager.StartSession(stub);

        Assert.True(manager.IsCapturing);
        Assert.NotNull(manager.CurrentSession);
        Assert.NotNull(provider);
    }

    [Fact]
    public void StartSession_WithSessionId_UsesProvidedId()
    {
        var manager = new CaptureSessionManager();
        var stub = new StubDataProvider();

        manager.StartSession(stub, "test-id");

        Assert.Equal("test-id", manager.CurrentSession!.SessionId);
    }

    [Fact]
    public void StartSession_ThrowsIfAlreadyCapturing()
    {
        var manager = new CaptureSessionManager();
        var stub = new StubDataProvider();

        manager.StartSession(stub);

        Assert.Throws<InvalidOperationException>(() => manager.StartSession(stub));
    }

    [Fact]
    public void EndSession_ReturnsCapturedCalls()
    {
        var manager = new CaptureSessionManager();
        var stub = new StubDataProvider();

        var provider = manager.StartSession(stub, "session-1");
        provider.ExecuteNonQuery("TestProc", 1);
        provider.ExecuteScalar("CountProc");

        var session = manager.EndSession();

        Assert.Equal("session-1", session.SessionId);
        Assert.Equal(2, session.Calls.Count);
        Assert.False(manager.IsCapturing);
    }

    [Fact]
    public void EndSession_ThrowsIfNotCapturing()
    {
        var manager = new CaptureSessionManager();

        Assert.Throws<InvalidOperationException>(() => manager.EndSession());
    }

    [Fact]
    public void ToJson_ProducesExpectedFormat()
    {
        var session = new CaptureSession
        {
            SessionId = "test-uuid",
            Calls = new List<CapturedCall>
            {
                new()
                {
                    Timestamp = new DateTime(2026, 3, 29, 10, 0, 0),
                    ExecuteMethod = "ExecuteScalar<Int32>",
                    ProcedureName = "dbo.dnn_AddContentItem",
                    Parameters = new List<CapturedParameter>
                    {
                        new() { Index = 0, Value = "Content", Type = "String" },
                        new() { Index = 1, Value = 1, Type = "Int32" }
                    },
                    ReturnValue = 5
                }
            }
        };

        var json = CaptureSessionManager.ToJson(session);

        Assert.Contains("\"sessionId\"", json);
        Assert.Contains("test-uuid", json);
        Assert.Contains("ExecuteScalar<Int32>", json);
        Assert.Contains("dbo.dnn_AddContentItem", json);
        Assert.Contains("\"returnValue\"", json);
    }

    [Fact]
    public void SaveToFile_And_LoadFromFile_RoundTrip()
    {
        var session = new CaptureSession
        {
            SessionId = "file-test",
            Calls = new List<CapturedCall>
            {
                new()
                {
                    Timestamp = new DateTime(2026, 3, 29, 10, 0, 0),
                    ExecuteMethod = "ExecuteReader",
                    ProcedureName = "dbo.dnn_GetUser",
                    Parameters = new List<CapturedParameter>
                    {
                        new() { Index = 0, Value = 1, Type = "Int32" }
                    }
                }
            }
        };

        var tempFile = Path.Combine(Path.GetTempPath(), $"capture_test_{Guid.NewGuid()}.json");
        try
        {
            CaptureSessionManager.SaveToFile(session, tempFile);

            Assert.True(File.Exists(tempFile));

            var loaded = CaptureSessionManager.LoadFromFile(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal("file-test", loaded.SessionId);
            Assert.Single(loaded.Calls);
            Assert.Equal("ExecuteReader", loaded.Calls[0].ExecuteMethod);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveToFile_CreatesDirectoryIfNotExists()
    {
        var session = new CaptureSession { SessionId = "dir-test" };
        var tempDir = Path.Combine(Path.GetTempPath(), $"capture_test_{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempDir, "output.json");

        try
        {
            CaptureSessionManager.SaveToFile(session, tempFile);

            Assert.True(Directory.Exists(tempDir));
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EndToEnd_CaptureAndSerialize()
    {
        var manager = new CaptureSessionManager();
        var stub = new StubDataProvider();

        var provider = manager.StartSession(stub, "e2e-test");

        // Execute some operations
        using (var reader = provider.ExecuteReader("GetItems", 1))
        {
            while (reader.Read()) { }
        }
        provider.ExecuteNonQuery("UpdateItem", 1, "Updated");
        var count = provider.ExecuteScalar<int>("CountItems");

        var session = manager.EndSession();
        var json = CaptureSessionManager.ToJson(session);

        // Verify JSON is valid and contains expected data
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("e2e-test", root.GetProperty("sessionId").GetString());
        Assert.Equal(3, root.GetProperty("calls").GetArrayLength());
    }

    private class StubDataProvider : DataProvider
    {
        public override string ConnectionString => "test";
        public override string DatabaseOwner => "dbo.";
        public override string ObjectQualifier => "dnn_";

        public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Rows.Add(1);
            return table.CreateDataReader();
        }

        public override void ExecuteNonQuery(string procedureName, params object[] commandParameters) { }
        public override object ExecuteScalar(string procedureName, params object[] commandParameters) => 10;
        public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
            => (T)Convert.ChangeType(ExecuteScalar(procedureName, commandParameters), typeof(T));
        public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters) => new();
        public override IDataReader ExecuteSQL(string sql) => new DataTable().CreateDataReader();
        public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters) => new DataTable().CreateDataReader();
        public override string ExecuteScript(string script) => string.Empty;
        public override string ExecuteScript(string script, bool useTransactions) => string.Empty;
    }
}
