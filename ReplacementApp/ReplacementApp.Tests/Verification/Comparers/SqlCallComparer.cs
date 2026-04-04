using ReplacementApp.Core.DataAccess;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Comparers;

/// <summary>
/// CapturingDataProviderでキャプチャしたSQL呼び出しと期待値を比較する。
/// プロシージャ名の完全一致、パラメータ数・値の一致を検証する。
/// </summary>
public class SqlCallComparer
{
    private readonly ComparisonOptions _options;

    public SqlCallComparer(ComparisonOptions? options = null)
    {
        _options = options ?? ComparisonOptions.Default;
    }

    public ComparisonResult Compare(
        IReadOnlyList<CapturedCall> actual,
        IReadOnlyList<ExpectedSqlCall> expected)
    {
        var result = new ComparisonResult();

        if (actual.Count != expected.Count)
        {
            result.AddDifference("SqlCalls.Count", expected.Count, actual.Count);
        }

        var count = Math.Min(actual.Count, expected.Count);
        for (var i = 0; i < count; i++)
        {
            CompareCall(result, $"Call[{i}]", actual[i], expected[i]);
        }

        return result;
    }

    private void CompareCall(ComparisonResult result, string path, CapturedCall actual, ExpectedSqlCall expected)
    {
        if (actual.ProcedureName != expected.ProcedureName)
        {
            result.AddDifference($"{path}.ProcedureName", expected.ProcedureName, actual.ProcedureName);
        }

        if (actual.Parameters.Count != expected.Parameters.Count)
        {
            result.AddDifference($"{path}.Parameters.Count", expected.Parameters.Count, actual.Parameters.Count);
            return;
        }

        for (var i = 0; i < actual.Parameters.Count; i++)
        {
            CompareParameter(result, $"{path}.Parameters[{i}]", actual.Parameters[i], expected.Parameters[i]);
        }
    }

    private void CompareParameter(ComparisonResult result, string path,
        CapturedParameter actual, ExpectedSqlParameter expected)
    {
        var actualValue = NormalizeValue(actual.Value);
        var expectedValue = NormalizeValue(expected.Value);

        if (!ValuesAreEquivalent(actualValue, expectedValue))
        {
            result.AddDifference($"{path}.Value", expected.Value, actual.Value);
        }
    }

    private object? NormalizeValue(object? value)
    {
        // JsonElement → .NET型
        value = JsonValueHelper.Normalize(value);

        // DBNull → null
        if (_options.TreatDbNullAsNull && value is DBNull)
            return null;

        // NullInteger → null
        if (_options.TreatNullIntegerAsNull && value is int intVal && intVal == _options.NullIntegerValue)
            return null;

        // 文字列Trim
        if (_options.TrimStrings && value is string s)
            return s.Trim();

        return value;
    }

    private bool ValuesAreEquivalent(object? actual, object? expected)
    {
        if (actual is null && expected is null) return true;
        if (actual is null || expected is null) return false;

        // 数値型の差異を吸収
        if (JsonValueHelper.IsNumeric(actual) && JsonValueHelper.IsNumeric(expected))
            return JsonValueHelper.NumericEquals(actual, expected);

        // DateTime許容差
        if (actual is DateTime dtActual && expected is DateTime dtExpected)
            return Math.Abs((dtActual - dtExpected).TotalMilliseconds) <= _options.DateTimeTolerance.TotalMilliseconds;

        // 文字列比較
        if (actual is string sActual && expected is string sExpected)
        {
            var comparison = _options.IgnoreStringCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(sActual, sExpected, comparison);
        }

        return actual.Equals(expected);
    }
}
