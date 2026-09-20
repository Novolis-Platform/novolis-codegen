using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Supplies consumer-specific context and hooks for one binding generation run.</summary>
/// <typeparam name="TPhase">The consumer's emit phase enum.</typeparam>
/// <typeparam name="TContext">The consumer's specialized emit context.</typeparam>
public sealed class BindingCodegenRun<TPhase, TContext>
    where TPhase : struct, Enum
    where TContext : BindingEmitContext
{
    /// <summary>The declared binding project and its emit jobs.</summary>
    public required BindingProject Project { get; init; }

    /// <summary>Filesystem, manifests, regeneration hint, and optional-job selection.</summary>
    public required BindingCodegenOptions Options { get; init; }

    /// <summary>Creates the consumer context for one emitted output.</summary>
    public required Func<BindingEmitJob, IManifestFragment, string, string, TContext> CreateContext { get; init; }

    /// <summary>Maps one job to the phase passed to Roslyn hooks.</summary>
    public required Func<BindingEmitJob, TPhase> SelectPhase { get; init; }

    /// <summary>Consumer hooks applied after source emission.</summary>
    public IReadOnlyList<ICodegenHook<TPhase, TContext>> Hooks { get; init; } = [];
}

/// <summary>Runs declared binding jobs through the standard Roslyn write pipeline.</summary>
/// <typeparam name="TPhase">The consumer's emit phase enum.</typeparam>
/// <typeparam name="TContext">The consumer's specialized emit context.</typeparam>
public sealed class BindingCodegenHost<TPhase, TContext>
    where TPhase : struct, Enum
    where TContext : BindingEmitContext
{
    /// <summary>Emits all selected jobs and returns zero when all jobs complete.</summary>
    /// <param name="run">The binding generation run.</param>
    /// <param name="log">Optional generation log.</param>
    /// <returns>Zero after all selected jobs are written.</returns>
    public int Generate(BindingCodegenRun<TPhase, TContext> run, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        BindingCodegenExecutor.ValidateCompanions(run.Project, run.Options.Environment);

        foreach (var job in BindingCodegenExecutor.FilterJobs(run.Project, run.Options.IncludeOptional))
        {
            if (job.Emitter.Strategy != job.Target.Strategy)
            {
                throw new InvalidOperationException(
                    $"Job '{job.Label}' uses {job.Emitter.Strategy} but targets {job.Target.Strategy}.");
            }

            var fragment = ResolveFragment(run.Options.Manifests, job);
            var outputPath = run.Options.Environment.Combine(job.Target.RelativePath);
            var fingerprint = fragment.Sha256Hex();
            var context = run.CreateContext(job, fragment, outputPath, fingerprint);
            var source = job.Emitter.Emit(new EmitRequest(fragment, fingerprint, job.Target, context));
            var phase = run.SelectPhase(job);

            RoslynEmitWriter<TPhase, TContext>.WriteFile(
                source,
                context,
                phase,
                run.Hooks,
                ToRoslynFormatPolicy(job.FormatPolicy));

            log?.WriteLine($"emit: {job.Label} -> {job.Target.RelativePath}");
        }

        return 0;
    }

    private static IManifestFragment ResolveFragment(IBindingManifestSource manifests, BindingEmitJob job) =>
        manifests.Fragments.FirstOrDefault(
            fragment => fragment.Kind == job.FragmentKind
                        && string.Equals(fragment.Id, job.FragmentId, StringComparison.Ordinal))
        ?? throw new InvalidOperationException(
            $"Missing manifest fragment '{job.FragmentId}' ({job.FragmentKind}) for job '{job.Label}'.");

    private static FormatPolicy ToRoslynFormatPolicy(BindingFormatPolicy policy) =>
        policy switch
        {
            BindingFormatPolicy.RoslynFormatter => FormatPolicy.RoslynFormatter,
            BindingFormatPolicy.NormalizeWhitespace => FormatPolicy.NormalizeWhitespace,
            _ => throw new ArgumentOutOfRangeException(nameof(policy)),
        };
}
