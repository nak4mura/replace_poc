using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VBAnalyzer.Models;

namespace VBAnalyzer.Capturing;

/// <summary>
/// 期待値テストスイートのJSON出力を担当する。
/// パス生成、ファイル出力、既存ファイルへの追記をサポートする。
/// </summary>
public class ExpectedValueWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _basePath;

    public ExpectedValueWriter(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// functionIdからJSON出力パスを生成する。
    /// functionId形式: "Namespace.ClassName.MethodName" → "expected_values/Namespace/ClassName/MethodName.json"
    /// </summary>
    public string ResolveOutputPath(string functionId)
    {
        var parts = functionId.Split('.');
        var relativePath = parts.Length switch
        {
            >= 3 => Path.Combine(parts[..^2].Aggregate(Path.Combine), parts[^2], $"{parts[^1]}.json"),
            2 => Path.Combine(parts[0], $"{parts[1]}.json"),
            _ => $"{functionId}.json"
        };
        return Path.Combine(_basePath, relativePath);
    }

    /// <summary>
    /// ExpectedTestSuiteをJSONファイルに保存する。
    /// </summary>
    public void Write(ExpectedTestSuite suite)
    {
        var filePath = ResolveOutputPath(suite.FunctionId);
        EnsureDirectoryExists(filePath);
        var json = JsonSerializer.Serialize(suite, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 指定パスにExpectedTestSuiteをJSONファイルに保存する。
    /// </summary>
    public void Write(ExpectedTestSuite suite, string filePath)
    {
        EnsureDirectoryExists(filePath);
        var json = JsonSerializer.Serialize(suite, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// 既存JSONファイルにテストケースを追記する。
    /// ファイルが存在しない場合は新規作成する。
    /// 同一IDのテストケースが存在する場合は上書きする。
    /// </summary>
    public void AppendTestCase(string functionId, ExpectedTestCase testCase)
    {
        var filePath = ResolveOutputPath(functionId);
        var suite = LoadOrCreate(filePath, functionId);

        var existingIndex = suite.TestCases.FindIndex(tc => tc.Id == testCase.Id);
        if (existingIndex >= 0)
        {
            suite.TestCases[existingIndex] = testCase;
        }
        else
        {
            suite.TestCases.Add(testCase);
        }

        Write(suite, filePath);
    }

    /// <summary>
    /// JSONファイルからExpectedTestSuiteを読み込む。
    /// </summary>
    public static ExpectedTestSuite? Load(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ExpectedTestSuite>(json, JsonOptions);
    }

    /// <summary>
    /// ExpectedTestSuiteをJSON文字列にシリアライズする。
    /// </summary>
    public static string ToJson(ExpectedTestSuite suite)
    {
        return JsonSerializer.Serialize(suite, JsonOptions);
    }

    private ExpectedTestSuite LoadOrCreate(string filePath, string functionId)
    {
        if (File.Exists(filePath))
        {
            var existing = Load(filePath);
            if (existing != null)
                return existing;
        }
        return new ExpectedTestSuite { FunctionId = functionId };
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
