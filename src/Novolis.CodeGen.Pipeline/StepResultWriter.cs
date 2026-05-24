using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

/// <summary>Reads and writes per-step <c>result.json</c> documents.</summary>
public static class StepResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Writes <paramref name="document"/> to <c>result.json</c> under <paramref name="stepDir"/>.</summary>
    /// <param name="stepDir">Step directory.</param>
    /// <param name="document">Result document.</param>
    public static void Write(string stepDir, StepResultDocument document)
    {
        Directory.CreateDirectory(stepDir);
        var path = Path.Combine(stepDir, "result.json");
        var json = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Reads <c>result.json</c> from <paramref name="stepDir"/> when present.</summary>
    /// <param name="stepDir">Step directory.</param>
    /// <returns>Deserialized document, or <see langword="null"/> when missing.</returns>
    public static StepResultDocument? TryRead(string stepDir)
    {
        var path = Path.Combine(stepDir, "result.json");
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<StepResultDocument>(File.ReadAllText(path), JsonOptions);
    }
}
