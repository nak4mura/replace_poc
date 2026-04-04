using System.Text.Json;
using ReplacementApp.Core.DataAccess;
using ReplacementApp.Tests.Verification.Comparers;
using ReplacementApp.Tests.Verification.Models;

namespace ReplacementApp.Tests.Verification.Tests;

public class SqlCallComparerTests
{
    private readonly SqlCallComparer _comparer = new();

    #region Helper Methods

    private static CapturedCall MakeCall(string procedure, params (object? value, string type)[] parameters)
    {
        return new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = "ExecuteReader",
            ProcedureName = procedure,
            Parameters = parameters.Select((p, i) => new CapturedParameter
            {
                Index = i,
                Value = p.value,
                Type = p.type
            }).ToList()
        };
    }

    private static ExpectedSqlCall MakeExpected(string procedure, params (object? value, string type)[] parameters)
    {
        return new ExpectedSqlCall
        {
            ProcedureName = procedure,
            Parameters = parameters.Select((p, i) => new ExpectedSqlParameter
            {
                Index = i,
                Value = p.value,
                Type = p.type
            }).ToList()
        };
    }

    /// <summary>JSON経由でobject?にデシリアライズし、JsonElementとして取得する</summary>
    private static object? ToJsonElement(object? value)
    {
        var json = JsonSerializer.Serialize(new { v = value });
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("v").Clone();
    }

    #endregion

    [Fact]
    public void IdenticalSingleCall_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_AddContentItem", ("Content", "String"), (1, "Int32"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_AddContentItem", ("Content", "String"), (1, "Int32"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void DifferentProcedureName_ReportsDifference()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_AddContentItem")
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_DeleteContentItem")
        };

        var result = _comparer.Compare(actual, expected);
        Assert.False(result.IsMatch);
        Assert.Contains("ProcedureName", result.Differences[0]);
    }

    [Fact]
    public void DifferentParameterCount_ReportsDifference()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", ("a", "String"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", ("a", "String"), ("b", "String"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.False(result.IsMatch);
        Assert.Contains("Parameters.Count", result.Differences[0]);
    }

    [Fact]
    public void DifferentParameterValue_ReportsDifference()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", ("Hello", "String"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", ("World", "String"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.False(result.IsMatch);
        Assert.Contains("Parameters[0].Value", result.Differences[0]);
    }

    [Fact]
    public void DifferentCallCount_ReportsDifference()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_A"),
            MakeCall("dbo.dnn_B")
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_A")
        };

        var result = _comparer.Compare(actual, expected);
        Assert.False(result.IsMatch);
        Assert.Contains("Count", result.Differences[0]);
    }

    [Fact]
    public void EmptyLists_Passes()
    {
        var result = _comparer.Compare(
            new List<CapturedCall>(),
            new List<ExpectedSqlCall>());
        Assert.True(result.IsMatch);
    }

    [Fact]
    public void DbNullVsNull_WithOption_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", (DBNull.Value, "DBNull"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (null, "DBNull"))
        };

        var comparer = new SqlCallComparer(new ComparisonOptions { TreatDbNullAsNull = true });
        var result = comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void NullIntegerVsNull_WithOption_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", (-1, "Int32"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (null, "Int32"))
        };

        var comparer = new SqlCallComparer(new ComparisonOptions { TreatNullIntegerAsNull = true });
        var result = comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void NullIntegerVsNull_WithoutOption_Fails()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", (-1, "Int32"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (null, "Int32"))
        };

        var comparer = new SqlCallComparer(new ComparisonOptions { TreatNullIntegerAsNull = false });
        var result = comparer.Compare(actual, expected);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void JsonElementInteger_VsInt_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", (42, "Int32"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (ToJsonElement(42), "Int32"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void JsonElementString_VsString_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", ("Hello", "String"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (ToJsonElement("Hello"), "String"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }

    [Fact]
    public void JsonElementBoolean_VsBool_Passes()
    {
        var actual = new List<CapturedCall>
        {
            MakeCall("dbo.dnn_Test", (true, "Boolean"))
        };
        var expected = new List<ExpectedSqlCall>
        {
            MakeExpected("dbo.dnn_Test", (ToJsonElement(true), "Boolean"))
        };

        var result = _comparer.Compare(actual, expected);
        Assert.True(result.IsMatch, string.Join("\n", result.Differences));
    }
}
