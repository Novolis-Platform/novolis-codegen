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

/// <summary>Describes one generated output file (class name, strategy, paths).</summary>
/// <param name="ClassName">Generated type name.</param>
/// <param name="Strategy">Emitter strategy.</param>
/// <param name="RelativePath">Output path relative to the repository root.</param>
/// <param name="Namespace">CLR namespace for the generated type.</param>
/// <param name="AssemblyName">Target assembly name.</param>
/// <param name="Optional">When <see langword="true"/>, the job may be skipped (for example optional raygui).</param>
public sealed record EmitTarget(
    string ClassName,
    EmitStrategy Strategy,
    string RelativePath,
    string Namespace,
    string AssemblyName,
    bool Optional = false);

/// <summary>Declares a non-generated companion file that must exist before emit.</summary>
/// <param name="RelativePath">Path relative to the repository root.</param>
/// <param name="Description">Human-readable reason the file is required.</param>
public sealed record CompanionDeclaration(
    string RelativePath,
    string Description);

/// <summary>Input to a single binding emitter invocation.</summary>
/// <param name="Fragment">Manifest fragment to emit from.</param>
/// <param name="ManifestSha256">Fingerprint embedded in generated headers.</param>
/// <param name="Target">Output target metadata.</param>
/// <param name="Context">Emit context (paths, environment, hints).</param>
public sealed record EmitRequest(
    IManifestFragment Fragment,
    string ManifestSha256,
    EmitTarget Target,
    BindingEmitContext Context);

/// <summary>Emits binding source from a manifest fragment for a specific <see cref="EmitStrategy"/>.</summary>
public interface IBindingEmitter
{
    /// <summary>Strategy implemented by this emitter.</summary>
    EmitStrategy Strategy { get; }

    /// <summary>Generates source text for <paramref name="request"/>.</summary>
    /// <param name="request">Emit request.</param>
    /// <returns>Generated C# source (not yet formatted).</returns>
    string Emit(EmitRequest request);
}

/// <summary>Options for a full binding codegen run.</summary>
public sealed class BindingCodegenOptions
{
    /// <summary>Filesystem environment.</summary>
    public required CodegenEnvironment Environment { get; init; }

    /// <summary>Manifest source.</summary>
    public required IBindingManifestSource Manifests { get; init; }

    /// <summary>When <see langword="true"/>, optional raygui jobs are included.</summary>
    public bool IncludeRaygui { get; init; }

    /// <summary>When <see langword="true"/>, manifest fingerprints are verified before emit.</summary>
    public bool VerifyManifest { get; init; } = true;

    /// <summary>Command printed when generated files drift from manifests.</summary>
    public string RegenerateHint { get; init; } =
        "dotnet run --project codegen/Novolis.Raylib.Pipeline -- run generate";

    /// <summary>Creates options for a physical repository and manifest set.</summary>
    /// <param name="repoRoot">Repository root.</param>
    /// <param name="manifests">Manifest source.</param>
    /// <returns>Configured options with raygui enabled.</returns>
    public static BindingCodegenOptions Physical(string repoRoot, IBindingManifestSource manifests) =>
        new()
        {
            Environment = CodegenEnvironment.Physical(repoRoot),
            Manifests = manifests,
            IncludeRaygui = true,
        };
}

/// <summary>Orchestrates a complete binding codegen pass for a repository.</summary>
public interface IBindingCodegenHost
{
    /// <summary>Runs all configured emit jobs.</summary>
    /// <param name="options">Codegen options.</param>
    /// <param name="log">Optional log writer.</param>
    /// <returns>Process exit code (0 on success).</returns>
    int GenerateAll(BindingCodegenOptions options, TextWriter? log = null);
}

/// <summary>Collects companion requirements and emit jobs for one binding project.</summary>
public sealed class BindingProject
{
    private readonly List<CompanionDeclaration> _companions = [];
    private readonly List<BindingEmitJob> _jobs = [];

    /// <summary>Required companion files that must exist on disk before emit.</summary>
    public IReadOnlyList<CompanionDeclaration> Companions => _companions;

    /// <summary>Emit jobs registered for this project.</summary>
    public IReadOnlyList<BindingEmitJob> Jobs => _jobs;

    /// <summary>Creates an empty project with the given name.</summary>
    /// <param name="name">Project name (for logging).</param>
    /// <returns>A new <see cref="BindingProject"/>.</returns>
    public static BindingProject Create(string name) => new() { Name = name };

    /// <summary>Project name used in logs and diagnostics.</summary>
    public required string Name { get; init; }

    /// <summary>Registers a required companion file.</summary>
    /// <param name="relativePath">Path relative to the repository root.</param>
    /// <param name="description">Why the file is required.</param>
    /// <returns><see langword="this"/> for chaining.</returns>
    public BindingProject RequireCompanion(string relativePath, string description)
    {
        _companions.Add(new CompanionDeclaration(relativePath, description));
        return this;
    }

    /// <summary>Registers an emit job.</summary>
    /// <param name="job">Job definition.</param>
    /// <returns><see langword="this"/> for chaining.</returns>
    public BindingProject AddJob(BindingEmitJob job)
    {
        _jobs.Add(job);
        return this;
    }

    /// <summary>Throws when a required companion file is missing.</summary>
    /// <param name="environment">Codegen environment.</param>
    /// <exception cref="FileNotFoundException">When a companion file is missing.</exception>
    public void ValidateCompanions(CodegenEnvironment environment)
    {
        foreach (var companion in _companions)
        {
            if (!environment.FileExists(companion.RelativePath))
            {
                throw new FileNotFoundException(
                    $"Required companion missing: {companion.RelativePath}",
                    environment.Combine(companion.RelativePath));
            }
        }
    }
}

/// <summary>One emit job within a <see cref="BindingProject"/>.</summary>
/// <param name="Label">Short label for logs.</param>
/// <param name="FragmentKind">Expected manifest fragment kind.</param>
/// <param name="FragmentId">Manifest fragment identifier.</param>
/// <param name="Emitter">Emitter implementation.</param>
/// <param name="Target">Output target.</param>
/// <param name="Optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
public sealed record BindingEmitJob(
    string Label,
    FragmentKind FragmentKind,
    string FragmentId,
    IBindingEmitter Emitter,
    EmitTarget Target,
    bool Optional = false);

/// <summary>Shared helpers for binding project validation and job filtering.</summary>
public static class BindingCodegenExecutor
{
    /// <summary>Validates companion files for <paramref name="project"/>.</summary>
    /// <param name="project">Binding project.</param>
    /// <param name="environment">Codegen environment.</param>
    public static void ValidateCompanions(BindingProject project, CodegenEnvironment environment) =>
        project.ValidateCompanions(environment);

    /// <summary>Returns jobs that should run given optional raygui inclusion.</summary>
    /// <param name="project">Binding project.</param>
    /// <param name="includeRaygui">When <see langword="true"/>, optional jobs are included.</param>
    /// <returns>Filtered jobs.</returns>
    public static IEnumerable<BindingEmitJob> FilterJobs(BindingProject project, bool includeRaygui) =>
        project.Jobs.Where(j => !j.Optional || includeRaygui);
}
