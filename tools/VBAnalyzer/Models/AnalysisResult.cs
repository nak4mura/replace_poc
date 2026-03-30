namespace VBAnalyzer.Models;

public class AnalysisResult
{
    public string FilePath { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public List<string> Imports { get; set; } = new();
    public List<MethodSignature> Methods { get; set; } = new();
    public List<DataProviderCall> DataProviderCalls { get; set; } = new();
    public List<EntityInfo> Entities { get; set; } = new();
    public List<string> ParseErrors { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}
