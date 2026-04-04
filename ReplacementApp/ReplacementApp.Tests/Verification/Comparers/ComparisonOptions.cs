namespace ReplacementApp.Tests.Verification.Comparers;

public class ComparisonOptions
{
    /// <summary>NullInteger (-1) をnullと同等として扱うか</summary>
    public bool TreatNullIntegerAsNull { get; set; } = true;

    /// <summary>NullIntegerとして扱う値 (DNN default is -1)</summary>
    public int NullIntegerValue { get; set; } = -1;

    /// <summary>DBNull.Valueをnullとして扱うか</summary>
    public bool TreatDbNullAsNull { get; set; } = true;

    /// <summary>DateTime比較時のトレランス</summary>
    public TimeSpan DateTimeTolerance { get; set; } = TimeSpan.FromMilliseconds(3);

    /// <summary>文字列比較時にTrimするか</summary>
    public bool TrimStrings { get; set; }

    /// <summary>文字列比較を大文字小文字区別なしにするか</summary>
    public bool IgnoreStringCase { get; set; }

    public static ComparisonOptions Default => new();
}
