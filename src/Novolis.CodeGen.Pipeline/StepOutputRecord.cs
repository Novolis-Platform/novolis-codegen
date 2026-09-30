using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

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
