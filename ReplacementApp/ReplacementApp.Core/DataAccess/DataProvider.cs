using System.Data;
using System.Data.Common;

namespace ReplacementApp.Core.DataAccess;

/// <summary>
/// DotNetNuke.Data.DataProviderと同じシグネチャの抽象基底クラス。
/// 置換先C#コードがDNNと同じAPI面でデータアクセスできるようにする。
/// </summary>
public abstract class DataProvider : IDisposable
{
    public abstract string ConnectionString { get; }
    public abstract string DatabaseOwner { get; }
    public abstract string ObjectQualifier { get; }

    public abstract IDataReader ExecuteReader(string procedureName, params object[] commandParameters);
    public abstract void ExecuteNonQuery(string procedureName, params object[] commandParameters);
    public abstract object? ExecuteScalar(string procedureName, params object[] commandParameters);
    public abstract T? ExecuteScalar<T>(string procedureName, params object[] commandParameters);
    public abstract DataSet ExecuteDataSet(string procedureName, params object[] commandParameters);
    public abstract IDataReader ExecuteSQL(string sql);
    public abstract IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters);

    /// <summary>
    /// プロシージャ名をDatabaseOwner + ObjectQualifier + ProcedureName形式に解決する。
    /// </summary>
    public string ResolveProcedureName(string procedureName)
    {
        return $"{DatabaseOwner}{ObjectQualifier}{procedureName}";
    }

    /// <summary>
    /// SQL文中の{databaseOwner}と{objectQualifier}トークンを置換する。
    /// </summary>
    public string ReplaceTokens(string sql)
    {
        return sql
            .Replace("{databaseOwner}", DatabaseOwner)
            .Replace("{objectQualifier}", ObjectQualifier);
    }

    public virtual void Dispose()
    {
    }
}
