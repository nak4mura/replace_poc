using System.Text.Json;
using ReplacementApp.Tests.Verification.Comparers;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Tests;

public class OutputComparerTests
{
    private readonly OutputComparer _comparer = new();

    private static ExpectedOutput MakeExpected(string type, object? value = null)
    {
        return new ExpectedOutput { Type = type, Value = value };
    }

    private static object? ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(new { v = value });
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("v").Clone();
    }

    [Fact]
    public void NullExpected_NullActual_Passes()
    {
        var result = _comparer.Compare(null, MakeExpected("Null"));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void NullExpected_NonNullActual_Fails()
    {
        var result = _comparer.Compare(42, MakeExpected("Null"));
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void IntMatch_Passes()
    {
        var result = _comparer.Compare(42, MakeExpected("Int32", ToJsonElement(42)));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void IntMismatch_Fails()
    {
        var result = _comparer.Compare(42, MakeExpected("Int32", ToJsonElement(99)));
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void StringMatch_Passes()
    {
        var result = _comparer.Compare("Hello", MakeExpected("String", ToJsonElement("Hello")));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void StringWithTrailingSpace_TrimEnabled_Passes()
    {
        var comparer = new OutputComparer(new ComparisonOptions { TrimStrings = true });
        var result = comparer.Compare("Hello  ", MakeExpected("String", ToJsonElement("Hello")));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void DateTimeWithinTolerance_Passes()
    {
        var dt1 = new DateTime(2026, 1, 1, 12, 0, 0, 0);
        var dt2 = new DateTime(2026, 1, 1, 12, 0, 0, 2); // 2ms差
        var result = _comparer.Compare(dt1, MakeExpected("DateTime", ToJsonElement(dt2)));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void DateTimeOutsideTolerance_Fails()
    {
        var dt1 = new DateTime(2026, 1, 1, 12, 0, 0, 0);
        var dt2 = new DateTime(2026, 1, 1, 12, 0, 1, 0); // 1秒差
        var result = _comparer.Compare(dt1, MakeExpected("DateTime", ToJsonElement(dt2)));
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void BoolMatch_Passes()
    {
        var result = _comparer.Compare(true, MakeExpected("Boolean", ToJsonElement(true)));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void ObjectProperties_Match_Passes()
    {
        var actual = new TestEntity { Id = 1, Name = "Test" };
        var expectedProps = new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Test" } };
        var result = _comparer.Compare(actual, MakeExpected("Object", ToJsonElement(expectedProps)));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void ObjectProperties_Mismatch_Fails()
    {
        var actual = new TestEntity { Id = 1, Name = "Test" };
        var expectedProps = new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Different" } };
        var result = _comparer.Compare(actual, MakeExpected("Object", ToJsonElement(expectedProps)));
        Assert.False(result.IsMatch);
        Assert.Contains("Name", result.Differences[0]);
    }

    [Fact]
    public void NullExpectedOutput_NullActual_Passes()
    {
        var result = _comparer.Compare(null, null);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public void DictionaryActual_Match_Passes()
    {
        var actual = new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Test" } };
        var expectedProps = new Dictionary<string, object?> { { "Id", 1 }, { "Name", "Test" } };
        var result = _comparer.Compare(actual, MakeExpected("Object", ToJsonElement(expectedProps)));
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
