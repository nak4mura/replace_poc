using Microsoft.Data.SqlClient;

namespace VBAnalyzer.TestInfrastructure;

/// <summary>
/// テストDBのセットアップ・ティアダウンを管理する。
/// テーブル作成、テストデータ投入、クリーンアップを提供する。
/// </summary>
public class TestDatabaseManager : IDisposable
{
    private readonly string _connectionString;
    private readonly SqlScriptRunner _scriptRunner;
    private readonly string _scriptsBasePath;
    private SqlConnection? _connection;

    /// <summary>
    /// テーブル作成順序（FK依存関係を考慮）
    /// </summary>
    private static readonly string[] TableCreationOrder =
    {
        "ContentTypes",
        "MetaData",
        "Taxonomy_VocabularyTypes",
        "Taxonomy_ScopeTypes",
        "Taxonomy_Vocabularies",
        "Taxonomy_Terms",
        "ContentItems",
        "ContentItems_MetaData",
        "ContentItems_Tags"
    };

    /// <summary>
    /// テーブル削除順序（FK依存関係の逆順）
    /// </summary>
    private static readonly string[] TableCleanupOrder =
    {
        "ContentItems_MetaData",
        "ContentItems_Tags",
        "ContentItems",
        "Taxonomy_Terms",
        "Taxonomy_Vocabularies",
        "Taxonomy_VocabularyTypes",
        "Taxonomy_ScopeTypes",
        "MetaData",
        "ContentTypes"
    };

    public TestDatabaseManager(
        string connectionString,
        string scriptsBasePath,
        string databaseOwner = "dbo.",
        string objectQualifier = "dnn_")
    {
        _connectionString = connectionString;
        _scriptsBasePath = scriptsBasePath;
        _scriptRunner = new SqlScriptRunner(databaseOwner, objectQualifier);
    }

    /// <summary>
    /// DB接続を取得する（遅延初期化）。
    /// </summary>
    public SqlConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    /// <summary>
    /// 全テーブルをFK依存順に作成する。
    /// </summary>
    public void CreateTables()
    {
        var connection = GetConnection();
        var tablesDir = Path.Combine(_scriptsBasePath, "Tables");

        foreach (var tableName in TableCreationOrder)
        {
            var scriptPath = Path.Combine(tablesDir, $"{tableName}.sql");
            if (File.Exists(scriptPath))
            {
                _scriptRunner.ExecuteScriptFile(connection, scriptPath);
            }
        }
    }

    /// <summary>
    /// TestSetupScript.sql を実行してテストデータを投入する。
    /// </summary>
    public void SeedTestData()
    {
        var connection = GetConnection();
        var scriptPath = Path.Combine(_scriptsBasePath, "TestSetupScript.sql");

        if (File.Exists(scriptPath))
        {
            _scriptRunner.ExecuteScriptFile(connection, scriptPath);
        }
    }

    /// <summary>
    /// カスタムSQLスクリプトを実行してテストデータを投入する。
    /// </summary>
    public void ExecuteSetupScript(string scriptPath)
    {
        var connection = GetConnection();
        _scriptRunner.ExecuteScriptFile(connection, scriptPath);
    }

    /// <summary>
    /// SQL文字列を直接実行する。
    /// </summary>
    public void ExecuteSql(string sql)
    {
        var connection = GetConnection();
        _scriptRunner.ExecuteScript(connection, sql);
    }

    /// <summary>
    /// 全テーブルのデータをFK依存逆順で削除する。
    /// </summary>
    public void CleanupData()
    {
        var connection = GetConnection();
        using var cmd = connection.CreateCommand();

        foreach (var tableName in TableCleanupOrder)
        {
            var qualifiedName = _scriptRunner.ReplaceTokens(
                "{databaseOwner}[{objectQualifier}" + tableName + "]");
            cmd.CommandText = $"IF OBJECT_ID('{qualifiedName}', 'U') IS NOT NULL DELETE FROM {qualifiedName}";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 全テーブルをFK依存逆順でDROPする。
    /// </summary>
    public void DropTables()
    {
        var connection = GetConnection();
        using var cmd = connection.CreateCommand();

        foreach (var tableName in TableCleanupOrder)
        {
            var qualifiedName = _scriptRunner.ReplaceTokens(
                "{databaseOwner}[{objectQualifier}" + tableName + "]");
            cmd.CommandText = $"IF OBJECT_ID('{qualifiedName}', 'U') IS NOT NULL DROP TABLE {qualifiedName}";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// フルセットアップ: テーブル作成 → テストデータ投入
    /// </summary>
    public void Setup()
    {
        CreateTables();
        SeedTestData();
    }

    /// <summary>
    /// フルティアダウン: データ削除 → テーブルDROP
    /// </summary>
    public void Teardown()
    {
        CleanupData();
        DropTables();
    }

    /// <summary>
    /// 指定テーブルのレコード数を取得する。
    /// </summary>
    public int GetRecordCount(string tableName)
    {
        var connection = GetConnection();
        using var cmd = connection.CreateCommand();
        var qualifiedName = _scriptRunner.ReplaceTokens(
            "{databaseOwner}[{objectQualifier}" + tableName + "]");
        cmd.CommandText = $"SELECT COUNT(*) FROM {qualifiedName}";
        return (int)cmd.ExecuteScalar();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
