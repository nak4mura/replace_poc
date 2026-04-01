using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VBAnalyzer.Models;

namespace VBAnalyzer.Capturing;

/// <summary>
/// キャプチャセッションのライフサイクル管理とJSON出力を提供する。
/// </summary>
public class CaptureSessionManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private CaptureSession? _currentSession;
    private CapturingDataProvider? _provider;

    public CaptureSession? CurrentSession => _currentSession;
    public bool IsCapturing => _currentSession != null;

    /// <summary>
    /// 新しいキャプチャセッションを開始する。
    /// </summary>
    public CapturingDataProvider StartSession(DataProvider innerProvider)
    {
        if (_currentSession != null)
            throw new InvalidOperationException("セッションが既に開始されています。EndSessionを呼び出してください。");

        _currentSession = new CaptureSession();
        _provider = new CapturingDataProvider(innerProvider);
        return _provider;
    }

    /// <summary>
    /// 指定したsessionIdで新しいキャプチャセッションを開始する。
    /// </summary>
    public CapturingDataProvider StartSession(DataProvider innerProvider, string sessionId)
    {
        if (_currentSession != null)
            throw new InvalidOperationException("セッションが既に開始されています。EndSessionを呼び出してください。");

        _currentSession = new CaptureSession { SessionId = sessionId };
        _provider = new CapturingDataProvider(innerProvider);
        return _provider;
    }

    /// <summary>
    /// セッションを終了し、キャプチャ結果を返す。
    /// </summary>
    public CaptureSession EndSession()
    {
        if (_currentSession == null || _provider == null)
            throw new InvalidOperationException("セッションが開始されていません。");

        _currentSession.Calls = _provider.Calls.ToList();
        var session = _currentSession;
        _currentSession = null;
        _provider = null;
        return session;
    }

    /// <summary>
    /// セッションをJSON文字列にシリアライズする。
    /// </summary>
    public static string ToJson(CaptureSession session)
    {
        return JsonSerializer.Serialize(session, JsonOptions);
    }

    /// <summary>
    /// セッションをJSONファイルに書き出す。
    /// </summary>
    public static void SaveToFile(CaptureSession session, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = ToJson(session);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// JSONファイルからセッションを読み込む。
    /// </summary>
    public static CaptureSession? LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<CaptureSession>(json, JsonOptions);
    }
}
