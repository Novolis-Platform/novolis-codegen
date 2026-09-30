using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

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
