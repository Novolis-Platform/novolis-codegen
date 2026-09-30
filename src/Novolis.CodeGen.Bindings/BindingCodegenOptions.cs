using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Options for a full binding codegen run.</summary>
public sealed class BindingCodegenOptions
{
    /// <summary>Filesystem environment.</summary>
    public required CodegenEnvironment Environment { get; init; }

    /// <summary>Manifest source.</summary>
    public required IBindingManifestSource Manifests { get; init; }

    /// <summary>When <see langword="true"/>, optional emit jobs are included.</summary>
    public bool IncludeOptional { get; init; }

    /// <summary>When <see langword="true"/>, manifest fingerprints are verified before emit.</summary>
    public bool VerifyManifest { get; init; } = true;

    /// <summary>Command printed when generated files drift from manifests.</summary>
    public required string RegenerateHint { get; init; }

    /// <summary>Creates options for a physical repository and manifest set.</summary>
    /// <param name="repoRoot">Repository root.</param>
    /// <param name="manifests">Manifest source.</param>
    /// <param name="regenerateHint">Command printed when generated output drifts.</param>
    /// <returns>Configured options.</returns>
    public static BindingCodegenOptions Physical(
        string repoRoot,
        IBindingManifestSource manifests,
        string regenerateHint) =>
        new()
        {
            Environment = CodegenEnvironment.Physical(repoRoot),
            Manifests = manifests,
            RegenerateHint = regenerateHint,
        };
}
