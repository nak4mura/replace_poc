using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace VBAnalyzer.TestInfrastructure;

/// <summary>
/// SQLスクリプトの実行を担当する。GOバッチ分割とトークン置換を行う。
/// </summary>
public class SqlScriptRunner
{
    private static readonly Regex GoDelimiterRegex = new(
        @"(?<=(?:[^\w]+|^))GO(?=(?: |\t)*?(?:\r?\n|$))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private readonly string _databaseOwner;
    private readonly string _objectQualifier;

    public SqlScriptRunner(string databaseOwner = "dbo.", string objectQualifier = "dnn_")
    {
        _databaseOwner = databaseOwner;
        _objectQualifier = objectQualifier;
    }

    /// <summary>
    /// SQLスクリプトを実行する。GOで分割し、トークンを置換してバッチ実行する。
    /// </summary>
    public void ExecuteScript(SqlConnection connection, string sqlScript)
    {
        var batches = GoDelimiterRegex.Split(sqlScript);

        using var cmd = connection.CreateCommand();
        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            cmd.CommandText = ReplaceTokens(trimmed);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// SQLスクリプトファイルを読み込んで実行する。
    /// </summary>
    public void ExecuteScriptFile(SqlConnection connection, string filePath)
    {
        var script = File.ReadAllText(filePath);
        ExecuteScript(connection, script);
    }

    /// <summary>
    /// {databaseOwner} と {objectQualifier} トークンを置換する。
    /// </summary>
    public string ReplaceTokens(string sqlScript)
    {
        return sqlScript
            .Replace("{databaseOwner}", _databaseOwner)
            .Replace("{objectQualifier}", _objectQualifier);
    }
}
