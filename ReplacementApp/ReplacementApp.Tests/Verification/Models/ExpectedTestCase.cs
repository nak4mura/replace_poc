using System.Text.Json;

namespace ReplacementApp.Tests.Verification.Models;

public class ExpectedTestCase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Id { get; set; } = string.Empty;
    public string? DbSetup { get; set; }
    public Dictionary<string, object?> Inputs { get; set; } = new();
    public ExpectedOutput? ExpectedOutput { get; set; }
    public List<ExpectedSqlCall> ExpectedSqlCalls { get; set; } = new();

    /// <summary>
    /// Inputs辞書から指定キーの値をT型に変換して返す。
    /// JSONデシリアライズ時にJsonElementになる値を適切に変換する。
    /// </summary>
    public T? GetInput<T>(string key)
    {
        if (!Inputs.TryGetValue(key, out var value))
            return default;

        if (value is T typed)
            return typed;

        if (value is JsonElement je)
            return JsonSerializer.Deserialize<T>(je.GetRawText(), JsonOptions);

        return (T?)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// Inputs辞書全体を単一のT型オブジェクトとしてデシリアライズする。
    /// </summary>
    public T? GetInputAs<T>()
    {
        var json = JsonSerializer.Serialize(Inputs, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public override string ToString() => Id;
}
