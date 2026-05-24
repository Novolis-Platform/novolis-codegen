namespace Novolis.CodeGen.Bindings;

public class BindingEmitContext
{
    public required string RepoRoot { get; init; }

    public required string OutputPath { get; init; }

    public required string ManifestPath { get; init; }

    public required string ManifestSha256 { get; init; }

    public required string RegenerateHint { get; init; }

    public DebugConfigFragment? DebugConfig { get; init; }
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
    byte[] ManifestBytes,
    string ManifestPath,
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
    public required string RepoRoot { get; init; }

    public bool IncludeRaygui { get; init; }

    public bool VerifyManifest { get; init; } = true;

    public string RegenerateHint { get; init; } =
        "dotnet run --project codegen/Novolis.Raylib.Pipeline -- run generate";
}

public interface IBindingCodegenHost
{
    int GenerateAll(BindingCodegenOptions options, TextWriter? log = null);
}

public sealed class BindingProject
{
    private readonly List<CompanionDeclaration> _companions = [];
    private readonly List<EmitJob> _jobs = [];

    public IReadOnlyList<CompanionDeclaration> Companions => _companions;

    public IReadOnlyList<EmitJob> Jobs => _jobs;

    public static BindingProject Create(string name) => new() { Name = name };

    public required string Name { get; init; }

    public BindingProject RequireCompanion(string relativePath, string description)
    {
        _companions.Add(new CompanionDeclaration(relativePath, description));
        return this;
    }

    public BindingProject AddJob(EmitJob job)
    {
        _jobs.Add(job);
        return this;
    }

    public void ValidateCompanions(string repoRoot)
    {
        foreach (var companion in _companions)
        {
            var full = Path.Combine(repoRoot, companion.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
                throw new FileNotFoundException($"Required companion missing: {companion.RelativePath}", full);
        }
    }
}

public sealed record EmitJob(
    string Label,
    Func<string, byte[]> ManifestLoader,
    IBindingEmitter Emitter,
    EmitTarget Target,
    bool Optional = false);

public static class BindingCodegenExecutor
{
    public static void ValidateCompanions(BindingProject project, string repoRoot) =>
        project.ValidateCompanions(repoRoot);

    public static IEnumerable<EmitJob> FilterJobs(BindingProject project, bool includeRaygui) =>
        project.Jobs.Where(j => !j.Optional || includeRaygui);
}
