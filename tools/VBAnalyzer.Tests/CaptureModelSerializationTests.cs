using System.Text.Json;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

public class CaptureModelSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void CaptureSession_RoundTrip_PreservesAllFields()
    {
        var session = new CaptureSession
        {
            SessionId = "test-session-id",
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

        var json = JsonSerializer.Serialize(session, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CaptureSession>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(session.SessionId, deserialized.SessionId);
        Assert.Single(deserialized.Calls);

        var call = deserialized.Calls[0];
        Assert.Equal("ExecuteScalar<Int32>", call.ExecuteMethod);
        Assert.Equal("dbo.dnn_AddContentItem", call.ProcedureName);
        Assert.Equal(2, call.Parameters.Count);
        Assert.Equal(0, call.Parameters[0].Index);
        Assert.Equal("Content", call.Parameters[0].Value?.ToString());
        Assert.Equal("String", call.Parameters[0].Type);
    }

    [Fact]
    public void CaptureSession_JsonFormat_MatchesExpectedSchema()
    {
        var session = new CaptureSession
        {
            SessionId = "uuid-test",
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

        var json = JsonSerializer.Serialize(session, JsonOptions);

        // Verify JSON contains expected camelCase property names
        Assert.Contains("\"sessionId\"", json);
        Assert.Contains("\"calls\"", json);
        Assert.Contains("\"timestamp\"", json);
        Assert.Contains("\"executeMethod\"", json);
        Assert.Contains("\"procedureName\"", json);
        Assert.Contains("\"parameters\"", json);
        Assert.Contains("\"index\"", json);
        Assert.Contains("\"value\"", json);
        Assert.Contains("\"type\"", json);
        Assert.Contains("\"returnValue\"", json);
    }

    [Fact]
    public void CaptureSession_EmptySession_SerializesCorrectly()
    {
        var session = new CaptureSession
        {
            SessionId = "empty-session"
        };

        var json = JsonSerializer.Serialize(session, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CaptureSession>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("empty-session", deserialized.SessionId);
        Assert.Empty(deserialized.Calls);
    }

    [Fact]
    public void CapturedCall_NullReturnValue_SerializesAsNull()
    {
        var call = new CapturedCall
        {
            Timestamp = DateTime.Now,
            ExecuteMethod = "ExecuteNonQuery",
            ProcedureName = "dbo.dnn_DeleteContentItem",
            ReturnValue = null
        };

        var json = JsonSerializer.Serialize(call, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CapturedCall>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.ReturnValue);
    }

    [Fact]
    public void CaptureSession_DefaultSessionId_IsValidGuid()
    {
        var session = new CaptureSession();

        Assert.True(Guid.TryParse(session.SessionId, out _));
    }
}
