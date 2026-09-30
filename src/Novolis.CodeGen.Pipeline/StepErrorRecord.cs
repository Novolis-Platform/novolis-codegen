using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

/// <summary>Error payload stored in <c>result.json</c>.</summary>
public sealed class StepErrorRecord
{
    /// <summary>Error message.</summary>
    public required string Message { get; init; }

    /// <summary>Exception type full name, when available.</summary>
    public string? Type { get; init; }
}
