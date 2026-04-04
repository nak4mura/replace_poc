using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ReplacementApp.Core.DataAccess;

/// <summary>
/// SQL Server実装のDataProvider。
/// DNN SqlDataProviderと同等の動作を提供する。
/// Microsoft.Data.SqlClientを使用してストアドプロシージャを呼び出す。
/// </summary>
public class SqlDataProvider : DataProvider
{
    private readonly string _connectionString;
    private readonly string _databaseOwner;
    private readonly string _objectQualifier;

    public SqlDataProvider(string connectionString, string databaseOwner, string objectQualifier)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _databaseOwner = databaseOwner ?? throw new ArgumentNullException(nameof(databaseOwner));
        _objectQualifier = objectQualifier ?? throw new ArgumentNullException(nameof(objectQualifier));
    }

    public override string ConnectionString => _connectionString;
    public override string DatabaseOwner => _databaseOwner;
    public override string ObjectQualifier => _objectQualifier;

    public override IDataReader ExecuteReader(string procedureName, params object[] commandParameters)
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = CreateCommand(connection, procedureName, commandParameters);
        // CommandBehavior.CloseConnection でリーダーclose時に接続も閉じる
        return command.ExecuteReader(CommandBehavior.CloseConnection);
    }

    public override void ExecuteNonQuery(string procedureName, params object[] commandParameters)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = CreateCommand(connection, procedureName, commandParameters);
        command.ExecuteNonQuery();
    }

    public override object? ExecuteScalar(string procedureName, params object[] commandParameters)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = CreateCommand(connection, procedureName, commandParameters);
        var result = command.ExecuteScalar();
        return result == DBNull.Value ? null : result;
    }

    public override T? ExecuteScalar<T>(string procedureName, params object[] commandParameters) where T : default
    {
        var result = ExecuteScalar(procedureName, commandParameters);
        if (result == null) return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public override DataSet ExecuteDataSet(string procedureName, params object[] commandParameters)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = CreateCommand(connection, procedureName, commandParameters);
        using var adapter = new SqlDataAdapter(command);
        var dataSet = new DataSet();
        adapter.Fill(dataSet);
        return dataSet;
    }

    public override IDataReader ExecuteSQL(string sql)
    {
        return ExecuteSQLInternal(sql, null);
    }

    public override IDataReader ExecuteSQL(string sql, params IDataParameter[] commandParameters)
    {
        return ExecuteSQLInternal(sql, commandParameters);
    }

    private IDataReader ExecuteSQLInternal(string sql, IDataParameter[]? commandParameters)
    {
        sql = ReplaceTokens(sql);

        var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        if (commandParameters != null)
        {
            foreach (var param in commandParameters)
            {
                command.Parameters.Add(param);
            }
        }

        return command.ExecuteReader(CommandBehavior.CloseConnection);
    }

    private SqlCommand CreateCommand(SqlConnection connection, string procedureName, object[] commandParameters)
    {
        var resolvedName = ResolveProcedureName(procedureName);
        var command = new SqlCommand(resolvedName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        // DNN SqlHelperと同様に、SPのパラメータ情報を取得してマッピング
        SqlCommandBuilder.DeriveParameters(command);

        // @RETURN_VALUE パラメータを除外してマッピング
        var spParams = command.Parameters.Cast<SqlParameter>()
            .Where(p => p.Direction != ParameterDirection.ReturnValue)
            .ToList();

        for (int i = 0; i < commandParameters.Length && i < spParams.Count; i++)
        {
            spParams[i].Value = commandParameters[i] ?? DBNull.Value;
        }

        return command;
    }

    public override void Dispose()
    {
        // SqlDataProviderは接続をメソッド単位で管理するため、Disposeで特別な処理は不要
    }
}
