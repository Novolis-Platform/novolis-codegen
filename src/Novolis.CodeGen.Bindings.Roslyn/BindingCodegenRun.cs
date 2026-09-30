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
