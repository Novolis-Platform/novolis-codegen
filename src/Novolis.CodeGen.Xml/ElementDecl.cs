namespace Novolis.CodeGen.Xml;

/// <summary>Global element declaration.</summary>
public sealed class ElementDecl
{
    /// <summary>Creates an element declaration.</summary>
    public ElementDecl(
        string name,
        string namespaceName,
        SchemaTypeId? typeId,
        bool isDocumentRoot,
        bool isNillable)
    {
        Name = name;
        NamespaceName = namespaceName;
        TypeId = typeId;
        IsDocumentRoot = isDocumentRoot;
        IsNillable = isNillable;
    }

    /// <summary>Local element name.</summary>
    public string Name { get; }

    /// <summary>Target namespace URI.</summary>
    public string NamespaceName { get; }

    /// <summary>Element type.</summary>
    public SchemaTypeId? TypeId { get; }

    /// <summary>Whether treated as a document root (no inbound refs / heuristic).</summary>
    public bool IsDocumentRoot { get; }

    /// <summary>Whether nillable.</summary>
    public bool IsNillable { get; }

    /// <summary>Qualified name string.</summary>
    public string QualifiedName =>
        string.IsNullOrEmpty(NamespaceName) ? Name : $"{{{NamespaceName}}}{Name}";
}
