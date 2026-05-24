using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

public sealed class PipelineContext
{
    public required IPipelineLayout Layout { get; init; }

    public required TextWriter Log { get; init; }

    public required bool Force { get; init; }

    public string RepoRoot => Layout.RepoRoot;

    public string StepsRoot => Layout.StepsRoot;

    public string StepDir(string stepId) => Layout.StepDir(stepId);

    public string StepArtifactsDir(string stepId) => Layout.StepArtifactsDir(stepId);
}

public enum StepStatus
{
    Pending,
    Skipped,
    Succeeded,
    Failed,
}

public sealed class StepExecutionResult
{
    public required StepStatus Status { get; init; }

    public IReadOnlyDictionary<string, string> Inputs { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<StepOutputRecord> Outputs { get; init; } = [];

    public string? SkipReason { get; init; }

    public StepErrorRecord? Error { get; init; }
}

public sealed class StepOutputRecord
{
    public required string Path { get; init; }

    public string? Sha256 { get; init; }

    public long? Bytes { get; init; }
}

public sealed class StepErrorRecord
{
    public required string Message { get; init; }

    public string? Type { get; init; }
}

public sealed class StepResultDocument
{
    public const string CurrentPipelineVersion = "1";

    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = "";

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepStatus Status { get; set; } = StepStatus.Pending;

    [JsonPropertyName("startedUtc")]
    public DateTimeOffset? StartedUtc { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    [JsonPropertyName("pipelineVersion")]
    public string PipelineVersion { get; set; } = CurrentPipelineVersion;

    [JsonPropertyName("inputs")]
    public Dictionary<string, string> Inputs { get; set; } = new(StringComparer.Ordinal);

    [JsonPropertyName("outputs")]
    public List<StepOutputRecord> Outputs { get; set; } = [];

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; set; }

    [JsonPropertyName("error")]
    public StepErrorRecord? Error { get; set; }
}
