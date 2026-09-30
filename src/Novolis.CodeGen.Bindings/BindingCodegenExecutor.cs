using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

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
