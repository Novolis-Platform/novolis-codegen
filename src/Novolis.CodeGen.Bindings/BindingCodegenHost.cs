using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Per-emit context passed to binding emitters and Roslyn hooks.</summary>
public class BindingEmitContext
{
    /// <summary>Repository filesystem environment.</summary>
    public required CodegenEnvironment Environment { get; init; }

    /// <summary>Absolute or repo-relative output path for the emitted file.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Manifest fragment driving this emit.</summary>
    public required IManifestFragment Fragment { get; init; }

    /// <summary>SHA-256 hex fingerprint of <see cref="Fragment"/>.</summary>
    public required string ManifestSha256 { get; init; }

    /// <summary>Human-readable command shown when generated output drifts.</summary>
    public required string RegenerateHint { get; init; }

    /// <summary>Optional debug configuration fragment for hook emitters.</summary>
    public DebugConfigFragment? DebugConfig { get; init; }

    /// <summary>Repository root path (shortcut for <see cref="Environment"/>.<see cref="CodegenEnvironment.RepoRoot"/>).</summary>
    public string RepoRoot => Environment.RepoRoot;
}
