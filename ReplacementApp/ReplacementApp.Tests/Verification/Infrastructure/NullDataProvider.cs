using System.Data;
using ReplacementApp.Core.DataAccess;

namespace ReplacementApp.Tests.Verification.Infrastructure;

/// <summary>
/// 単体テスト用のスタブDataProvider。
/// SQL実行は行わず、全メソッドがNotSupportedExceptionをスローする。
/// CapturingDataProviderと組み合わせてキャプチャのみ行う場合に使用する。
/// </summary>
public class NullDataProvider : DataProvider
{
    public override string ConnectionString => "not-connected";
    public override string DatabaseOwner => "dbo.";
    public override string ObjectQualifier => "dnn_";

    public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override void ExecuteNonQuery(string procedureName, params object[] commandParameters)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override object? ExecuteScalar(string procedureName, params object[] commandParameters)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override T? ExecuteScalar<T>(string procedureName, params object[] commandParameters) where T : default
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override IDataReader ExecuteSQL(string sql)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");

    public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
        => throw new NotSupportedException("NullDataProvider does not execute SQL");
}
