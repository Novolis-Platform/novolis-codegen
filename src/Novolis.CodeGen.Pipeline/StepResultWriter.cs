using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

public static class StepResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Write(string stepDir, StepResultDocument document)
    {
        Directory.CreateDirectory(stepDir);
        var path = Path.Combine(stepDir, "result.json");
        var json = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static StepResultDocument? TryRead(string stepDir)
    {
        var path = Path.Combine(stepDir, "result.json");
        if (!File.Exists(path))
            return null;

        return JsonSerializer.Deserialize<StepResultDocument>(File.ReadAllText(path), JsonOptions);
    }
}
