using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

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
