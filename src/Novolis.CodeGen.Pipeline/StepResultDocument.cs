using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

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
