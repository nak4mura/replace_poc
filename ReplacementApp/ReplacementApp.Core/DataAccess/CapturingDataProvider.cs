using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplacementApp.Core.DataAccess;

/// <summary>
/// DataProviderをラップし、全Execute*呼び出しをCapturedCallとして記録するデコレータ。
/// 検証テストでSQL呼び出しの一致を確認するために使用する。
/// </summary>
public class CapturingDataProvider : DataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly DataProvider _inner;
    private readonly List<CapturedCall> _calls = new();

    public CapturingDataProvider(DataProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<CapturedCall> CapturedCalls => _calls;

    public override string ConnectionString => _inner.ConnectionString;
    public override string DatabaseOwner => _inner.DatabaseOwner;
    public override string ObjectQualifier => _inner.ObjectQualifier;

    public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteReader", procedureName, commandParameters);
        var reader = _inner.ExecuteReader(procedureName, commandParameters);
        _calls.Add(call);
        return reader;
    }

    public override void ExecuteNonQuery(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteNonQuery", procedureName, commandParameters);
        _inner.ExecuteNonQuery(procedureName, commandParameters);
        _calls.Add(call);
    }

    public override object? ExecuteScalar(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteScalar", procedureName, commandParameters);
        var result = _inner.ExecuteScalar(procedureName, commandParameters);
        call.ReturnValue = result;
        _calls.Add(call);
        return result;
    }

    public override T? ExecuteScalar<T>(string procedureName, params object[] commandParameters) where T : default
    {
        var call = CreateCall($"ExecuteScalar<{typeof(T).Name}>", procedureName, commandParameters);
        var result = _inner.ExecuteScalar<T>(procedureName, commandParameters);
        call.ReturnValue = result;
        _calls.Add(call);
        return result;
    }

    public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteDataSet", procedureName, commandParameters);
        var result = _inner.ExecuteDataSet(procedureName, commandParameters);
        _calls.Add(call);
        return result;
    }

    public override IDataReader ExecuteSQL(string sql)
    {
        var call = new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = "ExecuteSQL",
            ProcedureName = sql
        };
        var reader = _inner.ExecuteSQL(sql);
        _calls.Add(call);
        return reader;
    }

    public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
    {
        var call = new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = "ExecuteSQL",
            ProcedureName = sql,
            Parameters = commandParameters.Select((p, i) => new CapturedParameter
            {
                Index = i,
                Value = p.Value,
                Type = p.DbType.ToString()
            }).ToList()
        };
        var reader = _inner.ExecuteSQL(sql, commandParameters);
        _calls.Add(call);
        return reader;
    }

    /// <summary>
    /// キャプチャした呼び出しをJSON文字列として出力する。
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(_calls, JsonOptions);
    }

    /// <summary>
    /// キャプチャした呼び出しをクリアする。
    /// </summary>
    public void Clear()
    {
        _calls.Clear();
    }

    public override void Dispose()
    {
        _inner.Dispose();
    }

    private CapturedCall CreateCall(string method, string procedureName, object[] parameters)
    {
        return new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = method,
            ProcedureName = ResolveProcedureName(procedureName),
            Parameters = parameters.Select((p, i) => new CapturedParameter
            {
                Index = i,
                Value = p is DBNull ? null : p,
                Type = p?.GetType().Name ?? "DBNull"
            }).ToList()
        };
    }
}
