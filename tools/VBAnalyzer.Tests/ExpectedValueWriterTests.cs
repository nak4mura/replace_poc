using System.Text.Json;
using VBAnalyzer.Capturing;
using VBAnalyzer.Models;
using Xunit;

namespace VBAnalyzer.Tests;

public class ExpectedValueWriterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ExpectedValueWriter _writer;

    public ExpectedValueWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"expected_values_test_{Guid.NewGuid()}");
        _writer = new ExpectedValueWriter(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ResolveOutputPath_ThreeParts_CreatesNestedPath()
    {
        var path = _writer.ResolveOutputPath("DotNetNuke.ContentController.AddContentItem");
        Assert.EndsWith(Path.Combine("DotNetNuke", "ContentController", "AddContentItem.json"), path);
    }

    [Fact]
    public void ResolveOutputPath_TwoParts_CreatesClassMethodPath()
    {
        var path = _writer.ResolveOutputPath("ContentController.AddContentItem");
        Assert.EndsWith(Path.Combine("ContentController", "AddContentItem.json"), path);
    }

    [Fact]
    public void ResolveOutputPath_OnePart_CreatesFlatPath()
    {
        var path = _writer.ResolveOutputPath("SimpleFunction");
        Assert.EndsWith("SimpleFunction.json", path);
    }

    [Fact]
    public void Write_CreatesDirectoryAndFile()
    {
        var suite = CreateSampleSuite();

        _writer.Write(suite);

        var filePath = _writer.ResolveOutputPath(suite.FunctionId);
        Assert.True(File.Exists(filePath));

        var loaded = ExpectedValueWriter.Load(filePath);
        Assert.NotNull(loaded);
        Assert.Equal("ContentController.AddContentItem", loaded.FunctionId);
        Assert.Single(loaded.TestCases);
    }

    [Fact]
    public void Write_JsonFormat_MatchesExpectedSchema()
    {
        var suite = CreateSampleSuite();

        _writer.Write(suite);

        var filePath = _writer.ResolveOutputPath(suite.FunctionId);
        var json = File.ReadAllText(filePath);

        // camelCase確認
        Assert.Contains("\"functionId\"", json);
        Assert.Contains("\"testCases\"", json);
        Assert.Contains("\"expectedSqlCalls\"", json);
        Assert.Contains("\"procedureName\"", json);

        // null省略確認
        Assert.DoesNotContain("\"dbSetup\"", json);
    }

    [Fact]
    public void AppendTestCase_NewFile_CreatesFileWithTestCase()
    {
        var testCase = new ExpectedTestCase
        {
            Id = "case_1",
            Inputs = new Dictionary<string, object?> { { "content", "Test" } },
            ExpectedOutput = new ExpectedOutput { Type = "Int32", Value = 1 }
        };

        _writer.AppendTestCase("ContentController.AddContentItem", testCase);

        var filePath = _writer.ResolveOutputPath("ContentController.AddContentItem");
        var loaded = ExpectedValueWriter.Load(filePath);
        Assert.NotNull(loaded);
        Assert.Single(loaded.TestCases);
        Assert.Equal("case_1", loaded.TestCases[0].Id);
    }

    [Fact]
    public void AppendTestCase_ExistingFile_AddsTestCase()
    {
        // 初回書き込み
        var suite = CreateSampleSuite();
        _writer.Write(suite);

        // 追記
        var newCase = new ExpectedTestCase
        {
            Id = "case_2",
            Inputs = new Dictionary<string, object?> { { "content", "New" } },
            ExpectedOutput = new ExpectedOutput { Type = "Int32", Value = 2 }
        };
        _writer.AppendTestCase("ContentController.AddContentItem", newCase);

        var filePath = _writer.ResolveOutputPath("ContentController.AddContentItem");
        var loaded = ExpectedValueWriter.Load(filePath);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.TestCases.Count);
        Assert.Equal("valid_content_item", loaded.TestCases[0].Id);
        Assert.Equal("case_2", loaded.TestCases[1].Id);
    }

    [Fact]
    public void AppendTestCase_DuplicateId_OverwritesExisting()
    {
        var suite = CreateSampleSuite();
        _writer.Write(suite);

        var updatedCase = new ExpectedTestCase
        {
            Id = "valid_content_item",
            Inputs = new Dictionary<string, object?> { { "content", "Updated" } },
            ExpectedOutput = new ExpectedOutput { Type = "Int32", Value = 99 }
        };
        _writer.AppendTestCase("ContentController.AddContentItem", updatedCase);

        var filePath = _writer.ResolveOutputPath("ContentController.AddContentItem");
        var loaded = ExpectedValueWriter.Load(filePath);
        Assert.NotNull(loaded);
        Assert.Single(loaded.TestCases);
        Assert.Equal(99, ((JsonElement)loaded.TestCases[0].ExpectedOutput!.Value!).GetInt32());
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsNull()
    {
        var result = ExpectedValueWriter.Load(Path.Combine(_tempDir, "nonexistent.json"));
        Assert.Null(result);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var suite = CreateSampleSuite();
        var json = ExpectedValueWriter.ToJson(suite);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("ContentController.AddContentItem",
            doc.RootElement.GetProperty("functionId").GetString());
    }

    private static ExpectedTestSuite CreateSampleSuite()
    {
        return new ExpectedTestSuite
        {
            FunctionId = "ContentController.AddContentItem",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "valid_content_item",
                    Inputs = new Dictionary<string, object?> { { "content", "TestContent" }, { "contentTypeId", 1 } },
                    ExpectedOutput = new ExpectedOutput { Type = "Int32", Value = 2 },
                    ExpectedSqlCalls = new List<ExpectedSqlCall>
                    {
                        new()
                        {
                            ProcedureName = "dbo.dnn_AddContentItem",
                            Parameters = new List<ExpectedSqlParameter>
                            {
                                new() { Index = 0, Value = "TestContent", Type = "String" }
                            }
                        }
                    }
                }
            }
        };
    }
}
