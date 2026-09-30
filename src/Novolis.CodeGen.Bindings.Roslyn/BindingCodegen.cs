using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Zero-ceremony entry point for consumers that do not need custom phases or hooks.</summary>
public static class BindingCodegen
{
    /// <summary>
    /// Emits all selected jobs using the default <see cref="BindingEmitContext"/> and no Roslyn hooks.
    /// </summary>
    /// <param name="project">Declared companions and emit jobs.</param>
    /// <param name="options">Filesystem, manifests, and regeneration hint.</param>
    /// <param name="log">Optional generation log.</param>
    /// <returns>Zero after all selected jobs are written.</returns>
    public static int Generate(BindingProject project, BindingCodegenOptions options, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);

        return new BindingCodegenHost<DefaultPhase, BindingEmitContext>().Generate(
            new BindingCodegenRun<DefaultPhase, BindingEmitContext>
            {
                Project = project,
                Options = options,
                SelectPhase = static _ => DefaultPhase.Emit,
                CreateContext = (_, fragment, outputPath, fingerprint) => new BindingEmitContext
                {
                    Environment = options.Environment,
                    OutputPath = outputPath,
                    Fragment = fragment,
                    ManifestSha256 = fingerprint,
                    RegenerateHint = options.RegenerateHint,
                },
            },
            log);
    }

    private enum DefaultPhase
    {
        Emit,
    }
}
