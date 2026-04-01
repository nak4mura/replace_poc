using System.Data;
using VBAnalyzer.Models;

namespace VBAnalyzer.Capturing;

/// <summary>
/// IDataReaderをラップし、読み取られた行・列を全てキャプチャするデコレータ。
/// Forward-onlyセマンティクスを維持しつつ、結果セットを記録する。
/// </summary>
public class CapturingDataReader : IDataReader
{
    private readonly IDataReader _inner;
    private readonly List<CapturedResultSet> _resultSets = new();
    private CapturedResultSet _currentResultSet;
    private bool _schemaInitialized;

    public CapturingDataReader(IDataReader inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _currentResultSet = new CapturedResultSet();
        _resultSets.Add(_currentResultSet);
    }

    public List<CapturedResultSet> ResultSets => _resultSets;

    // --- IDataReader control flow ---

    public bool Read()
    {
        var result = _inner.Read();
        if (result)
        {
            EnsureSchema();
            CaptureCurrentRow();
        }
        return result;
    }

    public bool NextResult()
    {
        var result = _inner.NextResult();
        if (result)
        {
            _currentResultSet = new CapturedResultSet();
            _resultSets.Add(_currentResultSet);
            _schemaInitialized = false;
        }
        return result;
    }

    public void Close() => _inner.Close();

    public void Dispose() => _inner.Dispose();

    // --- Schema/metadata ---

    public int FieldCount => _inner.FieldCount;

    public string GetName(int i) => _inner.GetName(i);

    public int GetOrdinal(string name) => _inner.GetOrdinal(name);

    public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);

    public Type GetFieldType(int i) => _inner.GetFieldType(i);

    public DataTable? GetSchemaTable() => _inner.GetSchemaTable();

    public int Depth => _inner.Depth;

    public bool IsClosed => _inner.IsClosed;

    public int RecordsAffected => _inner.RecordsAffected;

    // --- Value access (delegated) ---

    public object this[int i] => _inner[i];

    public object this[string name] => _inner[name];

    public object GetValue(int i) => _inner.GetValue(i);

    public int GetValues(object[] values) => _inner.GetValues(values);

    public bool IsDBNull(int i) => _inner.IsDBNull(i);

    public bool GetBoolean(int i) => _inner.GetBoolean(i);

    public byte GetByte(int i) => _inner.GetByte(i);

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

    public char GetChar(int i) => _inner.GetChar(i);

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);

    public Guid GetGuid(int i) => _inner.GetGuid(i);

    public short GetInt16(int i) => _inner.GetInt16(i);

    public int GetInt32(int i) => _inner.GetInt32(i);

    public long GetInt64(int i) => _inner.GetInt64(i);

    public float GetFloat(int i) => _inner.GetFloat(i);

    public double GetDouble(int i) => _inner.GetDouble(i);

    public string GetString(int i) => _inner.GetString(i);

    public decimal GetDecimal(int i) => _inner.GetDecimal(i);

    public DateTime GetDateTime(int i) => _inner.GetDateTime(i);

    public IDataReader GetData(int i) => _inner.GetData(i);

    // --- Private helpers ---

    private void EnsureSchema()
    {
        if (_schemaInitialized) return;

        for (var i = 0; i < _inner.FieldCount; i++)
        {
            _currentResultSet.ColumnNames.Add(_inner.GetName(i));
        }
        _schemaInitialized = true;
    }

    private void CaptureCurrentRow()
    {
        var row = new List<object?>(_inner.FieldCount);
        for (var i = 0; i < _inner.FieldCount; i++)
        {
            var value = _inner.IsDBNull(i) ? null : _inner.GetValue(i);
            row.Add(value);
        }
        _currentResultSet.Rows.Add(row);
    }
}
