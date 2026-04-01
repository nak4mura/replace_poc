using System.Data;
using VBAnalyzer.Capturing;
using Xunit;

namespace VBAnalyzer.Tests;

public class CapturingDataReaderTests
{
    [Fact]
    public void Read_CapturesAllRows()
    {
        using var table = CreateTestTable();
        using var inner = table.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        while (reader.Read()) { }

        var rs = reader.ResultSets[0];
        Assert.Equal(2, rs.Rows.Count);
        Assert.Equal(1, rs.Rows[0][0]);
        Assert.Equal("Alice", rs.Rows[0][1]);
        Assert.Equal(2, rs.Rows[1][0]);
        Assert.Equal("Bob", rs.Rows[1][1]);
    }

    [Fact]
    public void Read_CapturesColumnNames()
    {
        using var table = CreateTestTable();
        using var inner = table.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        reader.Read();

        var rs = reader.ResultSets[0];
        Assert.Equal(new[] { "Id", "Name" }, rs.ColumnNames);
    }

    [Fact]
    public void Read_DelegatesValueAccessCorrectly()
    {
        using var table = CreateTestTable();
        using var inner = table.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        reader.Read();

        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal("Alice", reader.GetString(1));
        Assert.Equal(1, reader["Id"]);
        Assert.Equal("Alice", reader["Name"]);
        Assert.Equal(2, reader.FieldCount);
        Assert.Equal("Id", reader.GetName(0));
        Assert.Equal(1, reader.GetOrdinal("Name"));
    }

    [Fact]
    public void Read_HandlesNullValues()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, DBNull.Value);

        using var inner = table.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        reader.Read();

        var rs = reader.ResultSets[0];
        Assert.Null(rs.Rows[0][1]);
        Assert.True(reader.IsDBNull(1));
    }

    [Fact]
    public void NextResult_CapturesMultipleResultSets()
    {
        var table1 = CreateTestTable();
        var table2 = new DataTable();
        table2.Columns.Add("Total", typeof(int));
        table2.Rows.Add(100);

        var dataSet = new DataSet();
        dataSet.Tables.Add(table1);
        dataSet.Tables.Add(table2);

        using var inner = dataSet.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        // Read first result set
        while (reader.Read()) { }

        // Move to second result set
        Assert.True(reader.NextResult());
        while (reader.Read()) { }

        Assert.Equal(2, reader.ResultSets.Count);

        var rs1 = reader.ResultSets[0];
        Assert.Equal(new[] { "Id", "Name" }, rs1.ColumnNames);
        Assert.Equal(2, rs1.Rows.Count);

        var rs2 = reader.ResultSets[1];
        Assert.Equal(new[] { "Total" }, rs2.ColumnNames);
        Assert.Single(rs2.Rows);
        Assert.Equal(100, rs2.Rows[0][0]);
    }

    [Fact]
    public void EmptyResultSet_CapturesNoRows()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));

        using var inner = table.CreateDataReader();
        using var reader = new CapturingDataReader(inner);

        Assert.False(reader.Read());
        Assert.Single(reader.ResultSets);
        Assert.Empty(reader.ResultSets[0].Rows);
    }

    private static DataTable CreateTestTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, "Alice");
        table.Rows.Add(2, "Bob");
        return table;
    }
}
