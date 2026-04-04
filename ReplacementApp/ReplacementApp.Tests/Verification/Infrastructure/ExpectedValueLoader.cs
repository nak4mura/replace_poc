using System.Text.Json;
using System.Text.Json.Serialization;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Infrastructure;

/// <summary>
/// 期待値JSONファイルを読み込む。
/// パス解決ロジックはVBAnalyzer.Capturing.ExpectedValueWriterと同一。
/// </summary>
public class ExpectedValueLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _basePath;

    public ExpectedValueLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// functionId (例: "ContentController.AddContentItem") からJSONファイルを読み込む。
    /// </summary>
    public ExpectedTestSuite Load(string functionId)
    {
        var filePath = ResolveFilePath(functionId);
        return LoadFromFile(filePath);
    }

    /// <summary>
    /// JSONファイルパスを直接指定して読み込む。
    /// </summary>
    public static ExpectedTestSuite LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                $"期待値JSONファイルが見つかりません: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ExpectedTestSuite>(json, JsonOptions)
               ?? throw new InvalidOperationException($"期待値JSONのデシリアライズに失敗しました: {filePath}");
    }

    /// <summary>
    /// functionIdからJSONファイルパスを解決する。
    /// ExpectedValueWriter.ResolveOutputPathと同一ロジック。
    /// "ContentController.AddContentItem" → "{basePath}/ContentController/AddContentItem.json"
    /// </summary>
    public string ResolveFilePath(string functionId)
    {
        var parts = functionId.Split('.');
        var relativePath = parts.Length switch
        {
            >= 3 => Path.Combine(
                Path.Combine(parts[..^2]),
                parts[^2],
                $"{parts[^1]}.json"),
            2 => Path.Combine(parts[0], $"{parts[1]}.json"),
            _ => $"{functionId}.json"
        };
        return Path.Combine(_basePath, relativePath);
    }
}
