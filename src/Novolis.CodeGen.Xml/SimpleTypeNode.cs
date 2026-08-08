namespace Novolis.CodeGen.Xml;

/// <summary>Simple type node (restriction / list / union).</summary>
public sealed class SimpleTypeNode
{
    /// <summary>Creates a simple type node.</summary>
    public SimpleTypeNode(
        SchemaTypeId id,
        string csharpName,
        SchemaTypeId? baseTypeId,
        string clrTypeName,
        BinaryFacet binaryFacet,
        IReadOnlyList<string> enumerationValues)
    {
        Id = id;
        CSharpName = csharpName;
        BaseTypeId = baseTypeId;
        ClrTypeName = clrTypeName;
        BinaryFacet = binaryFacet;
        EnumerationValues = enumerationValues;
    }

    /// <summary>Type identity.</summary>
    public SchemaTypeId Id { get; }

    /// <summary>Suggested C# type name.</summary>
    public string CSharpName { get; }

    /// <summary>Base simple type when known.</summary>
    public SchemaTypeId? BaseTypeId { get; }

    /// <summary>Mapped CLR type name (e.g. <c>string</c>, <c>byte[]</c>).</summary>
    public string ClrTypeName { get; }

    /// <summary>Binary content tagging.</summary>
    public BinaryFacet BinaryFacet { get; }

    /// <summary>Enumeration facet values when present.</summary>
    public IReadOnlyList<string> EnumerationValues { get; }
}
