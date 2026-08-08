namespace Novolis.CodeGen.Xml;

/// <summary>Immutable schema IR produced by <see cref="SchemaGraphBuilder"/>.</summary>
public sealed class SchemaGraph
{
    /// <summary>Creates a schema graph.</summary>
    public SchemaGraph(
        IReadOnlyList<ComplexTypeNode> complexTypes,
        IReadOnlyList<SimpleTypeNode> simpleTypes,
        IReadOnlyList<ElementDecl> elements)
    {
        ComplexTypes = complexTypes;
        SimpleTypes = simpleTypes;
        Elements = elements;
        ComplexById = complexTypes.ToDictionary(t => t.Id);
        SimpleById = simpleTypes.ToDictionary(t => t.Id);
    }

    /// <summary>Complex types in deterministic order.</summary>
    public IReadOnlyList<ComplexTypeNode> ComplexTypes { get; }

    /// <summary>Simple types in deterministic order.</summary>
    public IReadOnlyList<SimpleTypeNode> SimpleTypes { get; }

    /// <summary>Global elements in deterministic order.</summary>
    public IReadOnlyList<ElementDecl> Elements { get; }

    /// <summary>Complex type lookup.</summary>
    public IReadOnlyDictionary<SchemaTypeId, ComplexTypeNode> ComplexById { get; }

    /// <summary>Simple type lookup.</summary>
    public IReadOnlyDictionary<SchemaTypeId, SimpleTypeNode> SimpleById { get; }

    /// <summary>Document root elements.</summary>
    public IEnumerable<ElementDecl> DocumentRoots => Elements.Where(e => e.IsDocumentRoot);

    /// <summary>Ordered sequence of all type ids (complex then simple).</summary>
    public IReadOnlyList<SchemaTypeId> TypeIdSequence =>
        ComplexTypes.Select(t => t.Id).Concat(SimpleTypes.Select(t => t.Id)).ToArray();
}
