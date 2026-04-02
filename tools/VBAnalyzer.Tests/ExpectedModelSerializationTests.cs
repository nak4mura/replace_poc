using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

public class ExpectedModelSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void ExpectedTestSuite_RoundTrip_PreservesAllFields()
    {
        var suite = new ExpectedTestSuite
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
                        { "content", "TestContent" },
                        { "moduleId", 1 }
                    },
                    ExpectedOutput = new ExpectedOutput
                    {
                        Type = "Int32",
                        Value = 2
                    },
                    ExpectedSqlCalls = new List<ExpectedSqlCall>
                    {
                        new()
                        {
                            ProcedureName = "dbo.dnn_AddContentItem",
                            Parameters = new List<ExpectedSqlParameter>
                            {
                                new() { Index = 0, Value = "TestContent", Type = "String" },
                                new() { Index = 1, Value = 1, Type = "Int32" }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(suite, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ExpectedTestSuite>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("ContentController.AddContentItem", deserialized.FunctionId);
        Assert.Single(deserialized.TestCases);

        var tc = deserialized.TestCases[0];
        Assert.Equal("valid_content_item", tc.Id);
        Assert.Equal("scripts/content_setup.sql", tc.DbSetup);
        Assert.Equal(2, tc.Inputs.Count);
        Assert.NotNull(tc.ExpectedOutput);
        Assert.Equal("Int32", tc.ExpectedOutput.Type);
        Assert.Single(tc.ExpectedSqlCalls);

        var sqlCall = tc.ExpectedSqlCalls[0];
        Assert.Equal("dbo.dnn_AddContentItem", sqlCall.ProcedureName);
        Assert.Equal(2, sqlCall.Parameters.Count);
        Assert.Equal(0, sqlCall.Parameters[0].Index);
        Assert.Equal("TestContent", sqlCall.Parameters[0].Value?.ToString());
        Assert.Equal("String", sqlCall.Parameters[0].Type);
    }

    [Fact]
    public void ExpectedTestSuite_JsonFormat_UsesCamelCase()
    {
        var suite = new ExpectedTestSuite
        {
            FunctionId = "Test.Method",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "test1",
                    ExpectedOutput = new ExpectedOutput { Type = "String", Value = "ok" },
                    ExpectedSqlCalls = new List<ExpectedSqlCall>
                    {
                        new()
                        {
                            ProcedureName = "dbo.dnn_Test",
                            Parameters = new List<ExpectedSqlParameter>
                            {
                                new() { Index = 0, Value = "v", Type = "String" }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(suite, JsonOptions);

        Assert.Contains("\"functionId\"", json);
        Assert.Contains("\"testCases\"", json);
        Assert.Contains("\"expectedOutput\"", json);
        Assert.Contains("\"expectedSqlCalls\"", json);
        Assert.Contains("\"procedureName\"", json);
    }

    [Fact]
    public void ExpectedTestSuite_NullFields_OmittedFromJson()
    {
        var suite = new ExpectedTestSuite
        {
            FunctionId = "Test.Method",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "test_no_setup",
                    DbSetup = null,
                    ExpectedOutput = null
                }
            }
        };

        var json = JsonSerializer.Serialize(suite, JsonOptions);

        Assert.DoesNotContain("\"dbSetup\"", json);
        Assert.DoesNotContain("\"expectedOutput\"", json);
    }

    [Fact]
    public void ExpectedTestSuite_EmptySuite_SerializesCorrectly()
    {
        var suite = new ExpectedTestSuite
        {
            FunctionId = "Empty.Function"
        };

        var json = JsonSerializer.Serialize(suite, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ExpectedTestSuite>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("Empty.Function", deserialized.FunctionId);
        Assert.Empty(deserialized.TestCases);
    }

    [Fact]
    public void ExpectedTestSuite_DeserializeFromExpectedJson_MatchesIssueFormat()
    {
        var json = """
        {
          "functionId": "ContentController.AddContentItem",
          "testCases": [
            {
              "id": "valid_content_item",
              "dbSetup": "scripts/content_setup.sql",
              "inputs": { "content": "TestContent" },
              "expectedOutput": {"type": "Int32", "value": 2},
              "expectedSqlCalls": [
                {
                  "procedureName": "dbo.dnn_AddContentItem",
                  "parameters": [
                    {"index": 0, "value": "TestContent", "type": "String"}
                  ]
                }
              ]
            }
          ]
        }
        """;

        var suite = JsonSerializer.Deserialize<ExpectedTestSuite>(json, JsonOptions);

        Assert.NotNull(suite);
        Assert.Equal("ContentController.AddContentItem", suite.FunctionId);
        Assert.Single(suite.TestCases);

        var tc = suite.TestCases[0];
        Assert.Equal("valid_content_item", tc.Id);
        Assert.Equal("scripts/content_setup.sql", tc.DbSetup);
        Assert.NotNull(tc.ExpectedOutput);
        Assert.Equal("Int32", tc.ExpectedOutput!.Type);
        Assert.Single(tc.ExpectedSqlCalls);
        Assert.Equal("dbo.dnn_AddContentItem", tc.ExpectedSqlCalls[0].ProcedureName);
    }
}
