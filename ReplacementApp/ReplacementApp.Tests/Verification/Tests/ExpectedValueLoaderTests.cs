using System.Text.Json;
using ReplacementApp.Tests.Verification.Infrastructure;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Tests;

public class ExpectedValueLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ExpectedValueLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"loader_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ResolveFilePath_TwoParts_ReturnsCorrectPath()
    {
        var loader = new ExpectedValueLoader(_tempDir);
        var path = loader.ResolveFilePath("ContentController.AddContentItem");

        Assert.Equal(
            Path.Combine(_tempDir, "ContentController", "AddContentItem.json"),
            path);
    }

    [Fact]
    public void ResolveFilePath_ThreeParts_ReturnsCorrectPath()
    {
        var loader = new ExpectedValueLoader(_tempDir);
        var path = loader.ResolveFilePath("DotNetNuke.ContentController.AddContentItem");

        Assert.Equal(
            Path.Combine(_tempDir, "DotNetNuke", "ContentController", "AddContentItem.json"),
            path);
    }

    [Fact]
    public void Load_ValidJson_ReturnsSuite()
    {
        var suite = new ExpectedTestSuite
        {
            FunctionId = "Test.Method",
            TestCases = new List<ExpectedTestCase>
            {
                new()
                {
                    Id = "case1",
                    Inputs = new Dictionary<string, object?> { { "param1", 42 } },
                    ExpectedOutput = new ExpectedOutput { Type = "Int32", Value = 100 },
                    ExpectedSqlCalls = new List<ExpectedSqlCall>
                    {
                        new()
                        {
                            ProcedureName = "dbo.TestProc",
                            Parameters = new List<ExpectedSqlParameter>
                            {
                                new() { Index = 0, Value = 42, Type = "Int32" }
                            }
                        }
                    }
                }
            }
        };

        // JSONファイルを書き出す
        var dir = Path.Combine(_tempDir, "Test");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, "Method.json");
        var json = JsonSerializer.Serialize(suite, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);

        // 読み込み
        var loader = new ExpectedValueLoader(_tempDir);
        var loaded = loader.Load("Test.Method");

        Assert.Equal("Test.Method", loaded.FunctionId);
        Assert.Single(loaded.TestCases);
        Assert.Equal("case1", loaded.TestCases[0].Id);
        Assert.Single(loaded.TestCases[0].ExpectedSqlCalls);
        Assert.Equal("dbo.TestProc", loaded.TestCases[0].ExpectedSqlCalls[0].ProcedureName);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var loader = new ExpectedValueLoader(_tempDir);
        Assert.Throws<FileNotFoundException>(() => loader.Load("NonExistent.Method"));
    }

    [Fact]
    public void Load_FromRealTestData_Works()
    {
        // 出力ディレクトリのTestData/expected_values/を使う
        var assemblyDir = Path.GetDirectoryName(typeof(ExpectedValueLoaderTests).Assembly.Location)!;
        var basePath = Path.Combine(assemblyDir, "TestData", "expected_values");

        if (!Directory.Exists(basePath))
            return; // CI環境等でTestDataが無い場合はスキップ

        var loader = new ExpectedValueLoader(basePath);
        var suite = loader.Load("ContentController.AddContentItem");

        Assert.Equal("ContentController.AddContentItem", suite.FunctionId);
        Assert.NotEmpty(suite.TestCases);
        Assert.Equal("valid_content_item", suite.TestCases[0].Id);
    }

    [Fact]
    public void ExpectedValueDataAttribute_LoadsTestCases()
    {
        // DataAttributeが正しくテストケースを返すことを検証
        var attr = new ExpectedValueDataAttribute("ContentController.AddContentItem");
        var method = typeof(ExpectedValueLoaderTests).GetMethod(nameof(ExpectedValueDataAttribute_LoadsTestCases))!;

        var data = attr.GetData(method).ToList();

        Assert.NotEmpty(data);
        Assert.IsType<ExpectedTestCase>(data[0][0]);
        var testCase = (ExpectedTestCase)data[0][0];
        Assert.Equal("valid_content_item", testCase.Id);
    }
}
