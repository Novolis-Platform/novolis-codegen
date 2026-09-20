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
/// <param name="LibraryConstantName">Optional generated constant naming the native library.</param>
/// <param name="TypeSummary">Optional generated type XML summary.</param>
/// <param name="StructSummary">Optional generated struct XML summary.</param>
/// <param name="FacadeMethodImpl">Optional method implementation policy for façade forwards.</param>
public sealed record EmitTarget(
    string ClassName,
    EmitStrategy Strategy,
    string RelativePath,
    string Namespace,
    string AssemblyName,
    string? LibraryConstantName = null,
    string? TypeSummary = null,
    string? StructSummary = null,
    string? FacadeMethodImpl = null);

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
/// <param name="FormatPolicy">Formatting applied after the raw source is emitted.</param>
/// <param name="Slice">Optional named slice within a multi-output fragment.</param>
public sealed record BindingEmitJob(
    string Label,
    FragmentKind FragmentKind,
    string FragmentId,
    IBindingEmitter Emitter,
    EmitTarget Target,
    bool Optional = false,
    BindingFormatPolicy FormatPolicy = BindingFormatPolicy.RoslynFormatter,
    string? Slice = null)
{
    /// <summary>Creates a <see cref="LibraryImportEmitter"/> job for an interop exports fragment.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Interop fragment identifier.</param>
    /// <param name="className">Generated partial class name.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="libraryConstantName">Generated constant naming the native library.</param>
    /// <param name="typeSummary">Optional generated type XML summary.</param>
    /// <param name="structSummary">Optional generated struct XML summary.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured library-import emit job.</returns>
    public static BindingEmitJob LibraryImport(
        string label,
        string fragmentId,
        string className,
        string relativePath,
        string namespaceName,
        string assemblyName,
        string? libraryConstantName = null,
        string? typeSummary = null,
        string? structSummary = null,
        bool optional = false) =>
        new(
            label,
            FragmentKind.InteropExports,
            fragmentId,
            new LibraryImportEmitter(),
            new EmitTarget(
                className,
                EmitStrategy.LibraryImport,
                relativePath,
                namespaceName,
                assemblyName,
                libraryConstantName,
                typeSummary,
                structSummary),
            optional);

    /// <summary>Creates a <see cref="DynamicExportsEmitter"/> job for a shim exports fragment.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Shim fragment identifier.</param>
    /// <param name="className">Generated partial class name.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured dynamic-export emit job.</returns>
    public static BindingEmitJob DynamicExports(
        string label,
        string fragmentId,
        string className,
        string relativePath,
        string namespaceName,
        string assemblyName,
        bool optional = false) =>
        new(
            label,
            FragmentKind.ShimExports,
            fragmentId,
            new DynamicExportsEmitter(),
            new EmitTarget(
                className,
                EmitStrategy.DynamicExports,
                relativePath,
                namespaceName,
                assemblyName),
            optional);

    /// <summary>Creates a <see cref="FacadeForwardEmitter"/> job for one façade type slice.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Façade fragment identifier.</param>
    /// <param name="typeName">Façade type name and slice key.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="facadeMethodImpl">Optional method implementation policy such as <c>AggressiveInlining</c>.</param>
    /// <param name="documentation">Optional consumer documentation resolver.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured façade-forward emit job.</returns>
    public static BindingEmitJob FacadeForward(
        string label,
        string fragmentId,
        string typeName,
        string relativePath,
        string namespaceName,
        string assemblyName,
        string? facadeMethodImpl = null,
        IFacadeDocumentationResolver? documentation = null,
        bool optional = false) =>
        new(
            label,
            FragmentKind.FacadeTypes,
            fragmentId,
            new FacadeForwardEmitter(documentation),
            new EmitTarget(
                typeName,
                EmitStrategy.FacadeForward,
                relativePath,
                namespaceName,
                assemblyName,
                FacadeMethodImpl: facadeMethodImpl),
            optional,
            BindingFormatPolicy.NormalizeWhitespace,
            typeName);
}

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
    /// <param name="includeOptional">When <see langword="true"/>, optional jobs are included.</param>
    /// <returns>Filtered jobs.</returns>
    public static IEnumerable<BindingEmitJob> FilterJobs(BindingProject project, bool includeOptional) =>
        project.Jobs.Where(j => !j.Optional || includeOptional);
}
