using System.Text.Json;

namespace ReplacementApp.Tests.Verification.Comparers;

/// <summary>
/// JsonElementを.NET型に正規化するヘルパー。
/// System.Text.Jsonでobject?プロパティをデシリアライズするとJsonElementになるため、
/// 比較前に適切な.NET型に変換する。
/// </summary>
public static class JsonValueHelper
{
    public static object? Normalize(object? value)
    {
        if (value is not JsonElement je)
            return value;

        return je.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when je.TryGetInt32(out var i) => i,
            JsonValueKind.Number when je.TryGetInt64(out var l) => l,
            JsonValueKind.Number when je.TryGetDecimal(out var d) => d,
            JsonValueKind.String when je.TryGetDateTime(out var dt) => dt,
            JsonValueKind.String => je.GetString(),
            _ => je // Object/Array はJsonElementのまま返す
        };
    }

    /// <summary>
    /// 2つの値を比較可能な数値型に正規化して比較する。
    /// int, long, decimal間の差異を吸収する。
    /// </summary>
    public static bool NumericEquals(object a, object b)
    {
        try
        {
            var da = Convert.ToDecimal(a);
            var db = Convert.ToDecimal(b);
            return da == db;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsNumeric(object? value) =>
        value is int or long or short or byte or float or double or decimal;
}
