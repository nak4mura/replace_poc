namespace VBAnalyzer.Models;

public class CaptureSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public List<CapturedCall> Calls { get; set; } = new();
}
