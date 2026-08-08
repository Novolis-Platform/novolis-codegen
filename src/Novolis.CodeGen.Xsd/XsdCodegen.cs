using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>
/// Public facade for SchemaGraph → C# emit: choose a profile, options, and optional hooks
/// to mold the result (namespaces, spines, post-transforms).
/// </summary>
/// <remarks>
/// Orchestration / caching of multi-step regen belongs in <c>Novolis.CodeGen.Pipeline</c>
/// (implement <c>IPipelineStep</c> that calls <see cref="Emit"/>).
/// </remarks>
public static class XsdCodegen
{
    /// <summary>Emit with <paramref name="profile"/>, then apply hooks in <see cref="IXsdEmitHook.Order"/>.</summary>
    public static EmitResult Emit(IEmitProfile profile, SchemaGraph graph, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var result = profile.Emit(graph, options);
        var hooks = options.Hooks;
        if (hooks is null || hooks.Count == 0)
            return result;

        var context = new XsdEmitContext(options, profile);
        var ordered = hooks.OrderBy(h => h.Order).ToArray();
        var files = new List<EmittedFile>(result.Files.Count);
        foreach (var file in result.Files)
        {
            var current = file;
            foreach (var hook in ordered)
                current = hook.Transform(current, context);
            files.Add(current);
        }

        return new EmitResult(files);
    }

    /// <summary>Load schemas from a directory, optionally filter to document roots, then emit.</summary>
    public static EmitResult EmitFromDirectory(
        string schemaRoot,
        IEmitProfile profile,
        EmitOptions options,
        SchemaGraphOptions? graphOptions = null,
        IReadOnlySet<string>? scopeLocalNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaRoot);
        var graph = SchemaGraphBuilder.BuildFromDirectory(schemaRoot, graphOptions);
        if (scopeLocalNames is { Count: > 0 })
            graph = SchemaGraphScope.FilterToDocumentClosure(graph, scopeLocalNames);
        return Emit(profile, graph, options);
    }
}
