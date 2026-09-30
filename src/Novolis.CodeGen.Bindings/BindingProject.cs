using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

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
