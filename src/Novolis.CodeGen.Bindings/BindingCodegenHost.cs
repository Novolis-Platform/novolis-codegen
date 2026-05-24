using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

public class BindingEmitContext
{
    public required CodegenEnvironment Environment { get; init; }

    public required string OutputPath { get; init; }

    public required IManifestFragment Fragment { get; init; }

    public required string ManifestSha256 { get; init; }

    public required string RegenerateHint { get; init; }

    public DebugConfigFragment? DebugConfig { get; init; }

    public string RepoRoot => Environment.RepoRoot;
}

public sealed record EmitTarget(
    string ClassName,
    EmitStrategy Strategy,
    string RelativePath,
    string Namespace,
    string AssemblyName,
    bool Optional = false);

public sealed record CompanionDeclaration(
    string RelativePath,
    string Description);

public sealed record EmitRequest(
    IManifestFragment Fragment,
    string ManifestSha256,
    EmitTarget Target,
    BindingEmitContext Context);

public interface IBindingEmitter
{
    EmitStrategy Strategy { get; }

    string Emit(EmitRequest request);
}

public sealed class BindingCodegenOptions
{
    public required CodegenEnvironment Environment { get; init; }

    public required IBindingManifestSource Manifests { get; init; }

    public bool IncludeRaygui { get; init; }

    public bool VerifyManifest { get; init; } = true;

    public string RegenerateHint { get; init; } =
        "dotnet run --project codegen/Novolis.Raylib.Pipeline -- run generate";

    public static BindingCodegenOptions Physical(string repoRoot, IBindingManifestSource manifests) =>
        new()
        {
            Environment = CodegenEnvironment.Physical(repoRoot),
            Manifests = manifests,
            IncludeRaygui = true,
        };
}

public interface IBindingCodegenHost
{
    int GenerateAll(BindingCodegenOptions options, TextWriter? log = null);
}

public sealed class BindingProject
{
    private readonly List<CompanionDeclaration> _companions = [];
    private readonly List<BindingEmitJob> _jobs = [];

    public IReadOnlyList<CompanionDeclaration> Companions => _companions;

    public IReadOnlyList<BindingEmitJob> Jobs => _jobs;

    public static BindingProject Create(string name) => new() { Name = name };

    public required string Name { get; init; }

    public BindingProject RequireCompanion(string relativePath, string description)
    {
        _companions.Add(new CompanionDeclaration(relativePath, description));
        return this;
    }

    public BindingProject AddJob(BindingEmitJob job)
    {
        _jobs.Add(job);
        return this;
    }

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

public sealed record BindingEmitJob(
    string Label,
    FragmentKind FragmentKind,
    string FragmentId,
    IBindingEmitter Emitter,
    EmitTarget Target,
    bool Optional = false);

public static class BindingCodegenExecutor
{
    public static void ValidateCompanions(BindingProject project, CodegenEnvironment environment) =>
        project.ValidateCompanions(environment);

    public static IEnumerable<BindingEmitJob> FilterJobs(BindingProject project, bool includeRaygui) =>
        project.Jobs.Where(j => !j.Optional || includeRaygui);
}
