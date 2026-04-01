using System.Data;
using System.Data.Common;

namespace VBAnalyzer.Capturing;

/// <summary>
/// DotNetNuke.Data.DataProviderと同じシグネチャの抽象基底クラス。
/// .NET 8環境でCapturingDataProviderのベースとして使用する。
/// </summary>
public abstract class DataProvider
{
    public abstract string ConnectionString { get; }
    public abstract string DatabaseOwner { get; }
    public abstract string ObjectQualifier { get; }

    public abstract IDataReader ExecuteReader(string procedureName, params object[] commandParameters);
    public abstract void ExecuteNonQuery(string procedureName, params object[] commandParameters);
    public abstract object ExecuteScalar(string procedureName, params object[] commandParameters);
    public abstract T ExecuteScalar<T>(string procedureName, params object[] commandParameters);
    public abstract DataSet ExecuteDataSet(string procedureName, params object[] commandParameters);
    public abstract IDataReader ExecuteSQL(string sql);
    public abstract IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters);
    public abstract string ExecuteScript(string script);
    public abstract string ExecuteScript(string script, bool useTransactions);

    /// <summary>
    /// プロシージャ名をDatabaseOwner + ObjectQualifier + ProcedureName形式に解決する。
    /// </summary>
    public string ResolveProcedureName(string procedureName)
    {
        return $"{DatabaseOwner}{ObjectQualifier}{procedureName}";
    }
}
