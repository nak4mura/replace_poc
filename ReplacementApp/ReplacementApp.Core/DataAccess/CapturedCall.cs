namespace ReplacementApp.Core.DataAccess;

/// <summary>
/// キャプチャされたSQL呼び出しの記録。
/// VBAnalyzer.Models.CapturedCallと互換性のあるモデル。
/// </summary>
public class CapturedCall
{
    public DateTime Timestamp { get; set; }
    public string ExecuteMethod { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public List<CapturedParameter> Parameters { get; set; } = new();
    public object? ReturnValue { get; set; }
}

/// <summary>
/// キャプチャされたSQLパラメータ。
/// </summary>
public class CapturedParameter
{
    public int Index { get; set; }
    public object? Value { get; set; }
    public string Type { get; set; } = string.Empty;
}
