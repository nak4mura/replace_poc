namespace ReplacementApp.Tests.Verification.Comparers;

public class ComparisonResult
{
    public bool IsMatch => Differences.Count == 0;
    public List<string> Differences { get; } = new();

    public void AddDifference(string path, object? expected, object? actual)
    {
        Differences.Add($"[{path}] Expected: {Format(expected)}, Actual: {Format(actual)}");
    }

    public void AddDifference(string message)
    {
        Differences.Add(message);
    }

    /// <summary>
    /// 不一致があれば全差分を含むメッセージでAssert.Failする。
    /// </summary>
    public void AssertMatch()
    {
        if (!IsMatch)
        {
            Assert.Fail(
                $"Verification failed with {Differences.Count} difference(s):\n" +
                string.Join("\n", Differences));
        }
    }

    private static string Format(object? value) =>
        value is null ? "<null>" : $"{value} ({value.GetType().Name})";
}
