using VBAnalyzer.Models;
using VBAnalyzer.TestInfrastructure;

namespace VBAnalyzer.Capturing;

/// <summary>
/// VB関数を実行し、CapturingDataProvider経由でSQL呼び出しと出力値をキャプチャするハーネス。
/// DBセットアップ → 関数実行・キャプチャ → DBティアダウン の一連のフローを管理する。
/// </summary>
public class ExpectedValueCaptureHarness : IDisposable
{
    private readonly TestDatabaseManager _dbManager;
    private readonly CaptureSessionManager _sessionManager;

    public ExpectedValueCaptureHarness(
        string connectionString,
        string scriptsBasePath,
        string databaseOwner = "dbo.",
        string objectQualifier = "dnn_")
    {
        _dbManager = new TestDatabaseManager(connectionString, scriptsBasePath, databaseOwner, objectQualifier);
        _sessionManager = new CaptureSessionManager();
    }

    /// <summary>
    /// 対象関数を実行し、期待値テストケースを生成する。
    /// </summary>
    /// <param name="testCaseId">テストケースID</param>
    /// <param name="inputs">入力パラメータ</param>
    /// <param name="innerProvider">内部DataProvider（実際のDB接続用）</param>
    /// <param name="targetFunction">キャプチャ対象の関数。CapturingDataProviderが注入された状態で呼び出される。</param>
    /// <param name="dbSetupScript">カスタムDBセットアップスクリプトのパス（nullの場合はデフォルトのTestSetupScript.sqlを使用）</param>
    /// <returns>キャプチャされた期待値テストケース</returns>
    public ExpectedTestCase CaptureTestCase(
        string testCaseId,
        Dictionary<string, object?> inputs,
        DataProvider innerProvider,
        Func<CapturingDataProvider, object?> targetFunction,
        string? dbSetupScript = null)
    {
        // 1. DBセットアップ
        _dbManager.CreateTables();
        if (dbSetupScript != null)
        {
            _dbManager.ExecuteSetupScript(dbSetupScript);
        }
        else
        {
            _dbManager.SeedTestData();
        }

        try
        {
            // 2. CapturingDataProviderをセットアップ
            var capturingProvider = _sessionManager.StartSession(innerProvider);

            // 3. ComponentFactoryに登録
            ComponentFactory.RegisterComponentInstance<DataProvider>(capturingProvider);

            // 4. 対象関数を実行
            object? returnValue;
            try
            {
                returnValue = targetFunction(capturingProvider);
            }
            finally
            {
                // 5. セッション終了
                var session = _sessionManager.EndSession();
            }

            // 6. キャプチャ結果を期待値モデルに変換
            return new ExpectedTestCase
            {
                Id = testCaseId,
                DbSetup = dbSetupScript,
                Inputs = inputs,
                ExpectedOutput = CaptureToExpectedConverter.ConvertOutput(returnValue),
                ExpectedSqlCalls = CaptureToExpectedConverter.ConvertCalls(capturingProvider.Calls)
            };
        }
        finally
        {
            // 7. DBティアダウン
            _dbManager.CleanupData();
            ComponentFactory.Clear();
        }
    }

    /// <summary>
    /// 複数テストケースを実行し、ExpectedTestSuiteを生成する。
    /// </summary>
    public ExpectedTestSuite CaptureSuite(
        string functionId,
        DataProvider innerProvider,
        IEnumerable<TestCaseSpec> specs)
    {
        var suite = new ExpectedTestSuite { FunctionId = functionId };

        foreach (var spec in specs)
        {
            var testCase = CaptureTestCase(
                spec.Id, spec.Inputs, innerProvider, spec.TargetFunction, spec.DbSetupScript);
            suite.TestCases.Add(testCase);
        }

        return suite;
    }

    public void Dispose()
    {
        _dbManager.Dispose();
    }
}

/// <summary>
/// テストケースの仕様。CaptureSuiteに渡す各テストケースの定義。
/// </summary>
public class TestCaseSpec
{
    public required string Id { get; set; }
    public Dictionary<string, object?> Inputs { get; set; } = new();
    public required Func<CapturingDataProvider, object?> TargetFunction { get; set; }
    public string? DbSetupScript { get; set; }
}
