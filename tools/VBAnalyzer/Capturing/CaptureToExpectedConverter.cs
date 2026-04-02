using VBAnalyzer.Models;

namespace VBAnalyzer.Capturing;

/// <summary>
/// CapturedCall/CapturedParameter を ExpectedSqlCall/ExpectedSqlParameter に変換する。
/// </summary>
public static class CaptureToExpectedConverter
{
    /// <summary>
    /// CapturedCall のリストを ExpectedSqlCall のリストに変換する。
    /// </summary>
    public static List<ExpectedSqlCall> ConvertCalls(IEnumerable<CapturedCall> capturedCalls)
    {
        return capturedCalls.Select(ConvertCall).ToList();
    }

    /// <summary>
    /// 単一の CapturedCall を ExpectedSqlCall に変換する。
    /// </summary>
    public static ExpectedSqlCall ConvertCall(CapturedCall captured)
    {
        return new ExpectedSqlCall
        {
            ProcedureName = captured.ProcedureName,
            Parameters = captured.Parameters.Select(ConvertParameter).ToList()
        };
    }

    /// <summary>
    /// CapturedParameter を ExpectedSqlParameter に変換する。
    /// </summary>
    public static ExpectedSqlParameter ConvertParameter(CapturedParameter captured)
    {
        return new ExpectedSqlParameter
        {
            Index = captured.Index,
            Value = captured.Value,
            Type = captured.Type
        };
    }

    /// <summary>
    /// 戻り値を ExpectedOutput に変換する。
    /// </summary>
    public static ExpectedOutput ConvertOutput(object? returnValue)
    {
        if (returnValue == null || returnValue is DBNull)
        {
            return new ExpectedOutput { Type = "Null", Value = null };
        }

        return new ExpectedOutput
        {
            Type = returnValue.GetType().Name,
            Value = returnValue
        };
    }
}
