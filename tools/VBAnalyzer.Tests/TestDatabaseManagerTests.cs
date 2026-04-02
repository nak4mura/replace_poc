using VBAnalyzer.TestInfrastructure;
using Xunit;

namespace VBAnalyzer.Tests;

/// <summary>
/// TestDatabaseManagerの結合テスト。localDB上のDotNetNukeデータベースを使用する。
/// </summary>
[Collection("Database")]
public class TestDatabaseManagerTests : IDisposable
{
    private const string ConnectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True;TrustServerCertificate=True";

    private readonly string _scriptsBasePath;
    private readonly TestDatabaseManager _manager;

    public TestDatabaseManagerTests()
    {
        // replace_target/Library/Entities/Content/Data/Scripts/ へのパスを解決
        var projectRoot = FindProjectRoot();
        _scriptsBasePath = Path.Combine(projectRoot, "replace_target", "Library", "Entities", "Content", "Data", "Scripts");
        _manager = new TestDatabaseManager(ConnectionString, _scriptsBasePath);
    }

    [Fact]
    public void Setup_CreatesTablesAndSeedsData()
    {
        try
        {
            _manager.Setup();

            // テーブルにデータが投入されていることを確認
            Assert.True(_manager.GetRecordCount("ContentTypes") > 0);
            Assert.True(_manager.GetRecordCount("ContentItems") > 0);
            Assert.True(_manager.GetRecordCount("MetaData") > 0);
            Assert.True(_manager.GetRecordCount("Taxonomy_Terms") > 0);
        }
        finally
        {
            _manager.Teardown();
        }
    }

    [Fact]
    public void CleanupData_RemovesAllRecords()
    {
        try
        {
            _manager.Setup();
            _manager.CleanupData();

            // 全テーブルが空であることを確認
            Assert.Equal(0, _manager.GetRecordCount("ContentItems"));
            Assert.Equal(0, _manager.GetRecordCount("ContentTypes"));
            Assert.Equal(0, _manager.GetRecordCount("MetaData"));
        }
        finally
        {
            _manager.DropTables();
        }
    }

    [Fact]
    public void Teardown_DropsAllTables()
    {
        _manager.Setup();
        _manager.Teardown();

        // テーブルが存在しないことを確認
        var connection = _manager.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT OBJECT_ID('dbo.[dnn_ContentItems]', 'U')";
        var result = cmd.ExecuteScalar();
        Assert.True(result == null || result == DBNull.Value);
    }

    [Fact]
    public void ExecuteSql_RunsCustomSql()
    {
        try
        {
            _manager.CreateTables();
            _manager.ExecuteSql(
                "INSERT INTO dbo.[dnn_ContentTypes] ([ContentType]) VALUES ('CustomType')");

            Assert.Equal(1, _manager.GetRecordCount("ContentTypes"));
        }
        finally
        {
            _manager.Teardown();
        }
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("プロジェクトルートが見つかりません");
    }
}
