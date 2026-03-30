using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using VBAnalyzer.Analyzers;
using VBAnalyzer.Models;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

string? inputPath = null;
string? outputDir = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--input" or "-i" when i + 1 < args.Length:
            inputPath = args[++i];
            break;
        case "--output" or "-o" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--help" or "-h":
            PrintUsage();
            return 0;
        default:
            if (inputPath is null)
                inputPath = args[i];
            else if (outputDir is null)
                outputDir = args[i];
            break;
    }
}

if (inputPath is null || outputDir is null)
{
    Console.Error.WriteLine("Error: --input and --output are required.");
    PrintUsage();
    return 1;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
    return 1;
}

if (!inputPath.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Error: Input file must be a .vb file: {inputPath}");
    return 1;
}

Directory.CreateDirectory(outputDir);

var sourceText = File.ReadAllText(inputPath);
var tree = VisualBasicSyntaxTree.ParseText(sourceText, path: inputPath);
var diagnostics = tree.GetDiagnostics().ToList();

var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
foreach (var diag in errors)
{
    Console.Error.WriteLine($"Parse error: {diag}");
}

// Extract method signatures
var root = tree.GetRoot();
var methodExtractor = new MethodSignatureExtractor();
methodExtractor.Visit(root);

var result = new AnalysisResult
{
    FilePath = Path.GetFullPath(inputPath),
    Methods = methodExtractor.Methods.ToList(),
    ParseErrors = errors.Select(d => d.ToString()).ToList(),
    AnalyzedAt = DateTime.UtcNow
};

var outputFileName = Path.GetFileNameWithoutExtension(inputPath) + ".analysis.json";
var outputPath = Path.Combine(outputDir, outputFileName);
var json = JsonSerializer.Serialize(result, jsonOptions);
File.WriteAllText(outputPath, json);

Console.WriteLine($"Analysis complete: {outputPath}");
Console.WriteLine($"  Methods: {result.Methods.Count}");
Console.WriteLine($"  Parse errors: {errors.Count}");

return 0;

void PrintUsage()
{
    Console.WriteLine("Usage: VBAnalyzer --input <file.vb> --output <directory>");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -i, --input   Path to VB.NET source file");
    Console.WriteLine("  -o, --output  Output directory for analysis JSON");
    Console.WriteLine("  -h, --help    Show this help");
}
