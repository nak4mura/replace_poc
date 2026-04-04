using ReplacementApp.Core.DataAccess;
using ReplacementApp.Tests.Verification.Comparers;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Infrastructure;

/// <summary>
/// VB/C#動作一致検証テストの基底クラス。
/// CapturingDataProviderを自動有効化し、期待値との比較メソッドを提供する。
/// </summary>
public abstract class VerificationTestBase : IDisposable
{
    private readonly CapturingDataProvider _capturingProvider;
    private readonly ComparisonOptions _comparisonOptions;

    protected VerificationTestBase(ComparisonOptions? options = null)
    {
        _comparisonOptions = options ?? ComparisonOptions.Default;
        _capturingProvider = new CapturingDataProvider(CreateInnerProvider());
    }

    /// <summary>テスト対象コードに渡すDataProvider（キャプチャ有効）</summary>
    protected DataProvider DataProvider => _capturingProvider;

    /// <summary>
    /// 内部DataProviderを生成する。
    /// デフォルトはNullDataProvider。実DB接続が必要な場合はオーバーライドする。
    /// </summary>
    protected virtual DataProvider CreateInnerProvider() => new NullDataProvider();

    /// <summary>
    /// DB初期化スクリプトを実行する。
    /// 現時点では未実装。実DB統合時に実装する。
    /// </summary>
    protected void SetupDatabase(string? dbSetupScript)
    {
        if (string.IsNullOrEmpty(dbSetupScript)) return;
        // TODO: SqlScriptRunnerで実行（別Issue）
    }

    /// <summary>出力値を期待値と比較する。不一致があればAssert.Failする。</summary>
    protected void AssertOutputMatches(ExpectedOutput? expected, object? actual)
    {
        var comparer = new OutputComparer(_comparisonOptions);
        var result = comparer.Compare(actual, expected);
        result.AssertMatch();
    }

    /// <summary>キャプチャしたSQL呼び出しを期待値と比較する。不一致があればAssert.Failする。</summary>
    protected void AssertSqlCallsMatch(List<ExpectedSqlCall> expected)
    {
        var comparer = new SqlCallComparer(_comparisonOptions);
        var result = comparer.Compare(_capturingProvider.CapturedCalls, expected);
        result.AssertMatch();
    }

    /// <summary>キャプチャしたSQL呼び出しを取得する。</summary>
    protected IReadOnlyList<CapturedCall> GetCapturedCalls() => _capturingProvider.CapturedCalls;

    /// <summary>キャプチャをクリアする。</summary>
    protected void ClearCapturedCalls() => _capturingProvider.Clear();

    public virtual void Dispose()
    {
        _capturingProvider.Dispose();
    }
}
