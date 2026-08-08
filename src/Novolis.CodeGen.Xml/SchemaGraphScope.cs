namespace Novolis.CodeGen.Xml;

/// <summary>Filters a graph to the transitive closure of selected document roots.</summary>
public static class SchemaGraphScope
{
    /// <summary>Keeps document roots whose local names are in <paramref name="rootLocalNames"/> and all reachable types.</summary>
    public static SchemaGraph FilterToDocumentClosure(SchemaGraph graph, IReadOnlySet<string> rootLocalNames)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(rootLocalNames);

        var roots = graph.Elements
            .Where(e => rootLocalNames.Contains(e.Name) && e.TypeId is not null)
            .ToArray();
        var include = new HashSet<SchemaTypeId>();
        foreach (var root in roots)
            Walk(graph, root.TypeId!.Value, include);

        var complex = graph.ComplexTypes.Where(t => include.Contains(t.Id)).ToArray();
        var simple = graph.SimpleTypes.Where(t => include.Contains(t.Id)).ToArray();
        var elements = graph.Elements
            .Where(e => rootLocalNames.Contains(e.Name) || (e.TypeId is { } id && include.Contains(id)))
            .Select(e => new ElementDecl(
                e.Name,
                e.NamespaceName,
                e.TypeId,
                rootLocalNames.Contains(e.Name),
                e.IsNillable))
            .ToArray();

        return new SchemaGraph(complex, simple, elements);
    }

    private static void Walk(SchemaGraph graph, SchemaTypeId id, HashSet<SchemaTypeId> include)
    {
        if (!include.Add(id))
            return;

        if (graph.ComplexById.TryGetValue(id, out var complex))
        {
            if (complex.BaseTypeId is { } bas)
                Walk(graph, bas, include);
            foreach (var attr in complex.Attributes)
            {
                if (attr.TypeId is { } aid)
                    Walk(graph, aid, include);
            }

            WalkParticle(graph, complex.Particle, include);
        }
        else if (graph.SimpleById.TryGetValue(id, out var simple) && simple.BaseTypeId is { } sbas)
        {
            Walk(graph, sbas, include);
        }
    }

    private static void WalkParticle(SchemaGraph graph, Particle? particle, HashSet<SchemaTypeId> include)
    {
        if (particle is null)
            return;
        if (particle.TypeId is { } tid)
            Walk(graph, tid, include);
        foreach (var child in particle.Children)
            WalkParticle(graph, child, include);
    }
}
