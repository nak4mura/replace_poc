using System.Text.Json;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Comparers;

/// <summary>
/// C#置換関数の出力値を期待値と比較する。
/// プリミティブ型、エンティティオブジェクト、Dictionaryに対応する。
/// </summary>
public class OutputComparer
{
    private readonly ComparisonOptions _options;

    public OutputComparer(ComparisonOptions? options = null)
    {
        _options = options ?? ComparisonOptions.Default;
    }

    public ComparisonResult Compare(object? actual, ExpectedOutput? expected)
    {
        var result = new ComparisonResult();

        if (expected is null)
        {
            if (actual is not null)
                result.AddDifference("Output", "<null ExpectedOutput>", actual);
            return result;
        }

        if (expected.Type is "Null" or "null")
        {
            if (actual is not null)
                result.AddDifference("Output", "<null>", actual);
            return result;
        }

        if (actual is null)
        {
            result.AddDifference("Output", $"{expected.Type}: {expected.Value}", "<null>");
            return result;
        }

        if (expected.Type is "Object" or "object")
        {
            CompareObjectOutput(result, actual, expected.Value);
            return result;
        }

        // プリミティブ比較
        ComparePrimitiveOutput(result, actual, expected.Value);
        return result;
    }

    private void CompareObjectOutput(ComparisonResult result, object actual, object? expectedValue)
    {
        if (expectedValue is not JsonElement je || je.ValueKind != JsonValueKind.Object)
        {
            result.AddDifference("Output.Type", "JSON object", expectedValue?.GetType().Name ?? "<null>");
            return;
        }

        foreach (var prop in je.EnumerateObject())
        {
            var expectedPropValue = JsonValueHelper.Normalize(prop.Value.Clone());
            object? actualPropValue;

            if (actual is IDictionary<string, object?> dict)
            {
                dict.TryGetValue(prop.Name, out actualPropValue);
            }
            else
            {
                var propInfo = actual.GetType().GetProperty(prop.Name);
                if (propInfo is null)
                {
                    result.AddDifference($"Output.{prop.Name}", expectedPropValue, "<property not found>");
                    continue;
                }
                actualPropValue = propInfo.GetValue(actual);
            }

            if (!ValuesAreEquivalent(actualPropValue, expectedPropValue))
            {
                result.AddDifference($"Output.{prop.Name}", expectedPropValue, actualPropValue);
            }
        }
    }

    private void ComparePrimitiveOutput(ComparisonResult result, object actual, object? expectedValue)
    {
        var normalized = JsonValueHelper.Normalize(expectedValue);

        if (!ValuesAreEquivalent(actual, normalized))
        {
            result.AddDifference("Output.Value", normalized, actual);
        }
    }

    private bool ValuesAreEquivalent(object? actual, object? expected)
    {
        // null正規化
        if (_options.TreatDbNullAsNull)
        {
            if (actual is DBNull) actual = null;
            if (expected is DBNull) expected = null;
        }

        if (actual is null && expected is null) return true;
        if (actual is null || expected is null) return false;

        // 数値
        if (JsonValueHelper.IsNumeric(actual) && JsonValueHelper.IsNumeric(expected))
            return JsonValueHelper.NumericEquals(actual, expected);

        // DateTime
        if (actual is DateTime dtActual && expected is DateTime dtExpected)
            return Math.Abs((dtActual - dtExpected).TotalMilliseconds) <= _options.DateTimeTolerance.TotalMilliseconds;

        // 文字列
        if (actual is string sActual && expected is string sExpected)
        {
            if (_options.TrimStrings)
            {
                sActual = sActual.Trim();
                sExpected = sExpected.Trim();
            }
            var comparison = _options.IgnoreStringCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(sActual, sExpected, comparison);
        }

        return actual.Equals(expected);
    }
}
