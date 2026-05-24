using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

/// <summary>Runtime context for a single pipeline step execution.</summary>
public sealed class PipelineContext
{
    /// <summary>Repository layout.</summary>
    public required IPipelineLayout Layout { get; init; }

    /// <summary>Step log writer (typically <c>step.log</c>).</summary>
    public required TextWriter Log { get; init; }

    /// <summary>When <see langword="true"/>, skip detection is disabled.</summary>
    public required bool Force { get; init; }

    /// <summary>Repository root (shortcut for <see cref="Layout"/>.<see cref="IPipelineLayout.RepoRoot"/>).</summary>
    public string RepoRoot => Layout.RepoRoot;

    /// <summary>Steps root directory.</summary>
    public string StepsRoot => Layout.StepsRoot;

    /// <summary>Resolves a step directory path.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute step directory.</returns>
    public string StepDir(string stepId) => Layout.StepDir(stepId);

    /// <summary>Resolves a step artifacts directory path.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute artifacts directory.</returns>
    public string StepArtifactsDir(string stepId) => Layout.StepArtifactsDir(stepId);
}

/// <summary>Outcome of a pipeline step.</summary>
public enum StepStatus
{
    /// <summary>Step has not run yet.</summary>
    Pending,

    /// <summary>Step was skipped because inputs and outputs were unchanged.</summary>
    Skipped,

    /// <summary>Step completed successfully.</summary>
    Succeeded,

    /// <summary>Step failed.</summary>
    Failed,
}

/// <summary>Result returned from <see cref="IPipelineStep.ExecuteAsync"/>.</summary>
public sealed class StepExecutionResult
{
    /// <summary>Step outcome.</summary>
    public required StepStatus Status { get; init; }

    /// <summary>Input path to SHA-256 map (relative paths).</summary>
    public IReadOnlyDictionary<string, string> Inputs { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Output files produced by the step.</summary>
    public IReadOnlyList<StepOutputRecord> Outputs { get; init; } = [];

    /// <summary>Reason the step was skipped (when <see cref="Status"/> is <see cref="StepStatus.Skipped"/>).</summary>
    public string? SkipReason { get; init; }

    /// <summary>Error details (when <see cref="Status"/> is <see cref="StepStatus.Failed"/>).</summary>
    public StepErrorRecord? Error { get; init; }
}

/// <summary>One output file recorded in <c>result.json</c>.</summary>
public sealed class StepOutputRecord
{
    /// <summary>Path relative to the repository or step artifacts folder.</summary>
    public required string Path { get; init; }

    /// <summary>SHA-256 hex digest of the file contents.</summary>
    public string? Sha256 { get; init; }

    /// <summary>File size in bytes.</summary>
    public long? Bytes { get; init; }
}

/// <summary>Error payload stored in <c>result.json</c>.</summary>
public sealed class StepErrorRecord
{
    /// <summary>Error message.</summary>
    public required string Message { get; init; }

    /// <summary>Exception type full name, when available.</summary>
    public string? Type { get; init; }
}

/// <summary>Serialized step result written to <c>result.json</c>.</summary>
public sealed class StepResultDocument
{
    /// <summary>Current pipeline result schema version.</summary>
    public const string CurrentPipelineVersion = "1";

    /// <summary>Step identifier.</summary>
    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = "";

    /// <summary>Step outcome.</summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepStatus Status { get; set; } = StepStatus.Pending;

    /// <summary>UTC timestamp when the step started.</summary>
    [JsonPropertyName("startedUtc")]
    public DateTimeOffset? StartedUtc { get; set; }

    /// <summary>Elapsed milliseconds for the step.</summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>Pipeline result schema version.</summary>
    [JsonPropertyName("pipelineVersion")]
    public string PipelineVersion { get; set; } = CurrentPipelineVersion;

    /// <summary>Input path to SHA-256 map.</summary>
    [JsonPropertyName("inputs")]
    public Dictionary<string, string> Inputs { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Output file records.</summary>
    [JsonPropertyName("outputs")]
    public List<StepOutputRecord> Outputs { get; set; } = [];

    /// <summary>Skip reason when status is skipped.</summary>
    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; set; }

    /// <summary>Error details when status is failed.</summary>
    [JsonPropertyName("error")]
    public StepErrorRecord? Error { get; set; }
}
