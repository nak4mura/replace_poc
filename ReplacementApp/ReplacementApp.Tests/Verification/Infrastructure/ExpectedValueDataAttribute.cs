using System.Reflection;
using ReplacementApp.Tests.Verification.Models;
using Xunit.Sdk;

namespace ReplacementApp.Tests.Verification.Infrastructure;

/// <summary>
/// xUnit Theoryで期待値JSONからテストケースを供給するDataAttribute。
/// [ExpectedValueData("ContentController.AddContentItem")] のように使用する。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExpectedValueDataAttribute : DataAttribute
{
    private readonly string _functionId;

    public ExpectedValueDataAttribute(string functionId)
    {
        _functionId = functionId;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var basePath = ResolveBasePath();
        var loader = new ExpectedValueLoader(basePath);
        var suite = loader.Load(_functionId);

        foreach (var testCase in suite.TestCases)
        {
            yield return new object[] { testCase };
        }
    }

    private static string ResolveBasePath()
    {
        // テストアセンブリの出力ディレクトリからTestData/expected_values/を探す
        var assemblyDir = Path.GetDirectoryName(typeof(ExpectedValueDataAttribute).Assembly.Location)!;
        var path = Path.Combine(assemblyDir, "TestData", "expected_values");

        if (Directory.Exists(path))
            return path;

        // 環境変数によるオーバーライド
        var envPath = Environment.GetEnvironmentVariable("VERIFICATION_TEST_DATA");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        throw new DirectoryNotFoundException(
            $"期待値テストデータディレクトリが見つかりません: {path}。" +
            "TestData/expected_values/ が出力ディレクトリにコピーされているか確認してください。");
    }
}
