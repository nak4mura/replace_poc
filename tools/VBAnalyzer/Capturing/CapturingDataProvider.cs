using System.Data;
using System.Data.Common;
using VBAnalyzer.Models;

namespace VBAnalyzer.Capturing;

/// <summary>
/// DataProviderをラップし、全Execute*呼び出しをCapturedCallとして記録するデコレータ。
/// </summary>
public class CapturingDataProvider : DataProvider
{
    private readonly DataProvider _inner;
    private readonly List<CapturedCall> _calls = new();

    public CapturingDataProvider(DataProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<CapturedCall> Calls => _calls;

    public override string ConnectionString => _inner.ConnectionString;
    public override string DatabaseOwner => _inner.DatabaseOwner;
    public override string ObjectQualifier => _inner.ObjectQualifier;

    public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteReader", procedureName, commandParameters);
        var reader = _inner.ExecuteReader(procedureName, commandParameters);
        var capturingReader = new CapturingDataReader(reader);
        call.ResultSets = capturingReader.ResultSets;
        _calls.Add(call);
        return capturingReader;
    }

    public override void ExecuteNonQuery(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteNonQuery", procedureName, commandParameters);
        _inner.ExecuteNonQuery(procedureName, commandParameters);
        _calls.Add(call);
    }

    public override object ExecuteScalar(string procedureName, params object[] commandParameters)
    {
        var call = CreateCall("ExecuteScalar", procedureName, commandParameters);
        var result = _inner.ExecuteScalar(procedureName, commandParameters);
        call.ReturnValue = result;
        _calls.Add(call);
        return result;
    }

    public override T ExecuteScalar<T>(string procedureName, params object[] commandParameters)
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
        var capturingReader = new CapturingDataReader(reader);
        call.ResultSets = capturingReader.ResultSets;
        _calls.Add(call);
        return capturingReader;
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
        var capturingReader = new CapturingDataReader(reader);
        call.ResultSets = capturingReader.ResultSets;
        _calls.Add(call);
        return capturingReader;
    }

    public override string ExecuteScript(string script)
    {
        var call = new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = "ExecuteScript",
            ProcedureName = script
        };
        var result = _inner.ExecuteScript(script);
        call.ReturnValue = result;
        _calls.Add(call);
        return result;
    }

    public override string ExecuteScript(string script, bool useTransactions)
    {
        var call = new CapturedCall
        {
            Timestamp = DateTime.UtcNow,
            ExecuteMethod = "ExecuteScript",
            ProcedureName = script,
            Parameters = new List<CapturedParameter>
            {
                new() { Index = 0, Value = useTransactions, Type = "Boolean" }
            }
        };
        var result = _inner.ExecuteScript(script, useTransactions);
        call.ReturnValue = result;
        _calls.Add(call);
        return result;
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
