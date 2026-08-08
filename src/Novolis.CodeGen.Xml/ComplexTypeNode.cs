namespace Novolis.CodeGen.Xml;

/// <summary>Global or anonymous complex type node.</summary>
public sealed class ComplexTypeNode
{
    /// <summary>Creates a complex type node.</summary>
    public ComplexTypeNode(
        SchemaTypeId id,
        string csharpName,
        Particle? particle,
        IReadOnlyList<AttributeDecl> attributes,
        SchemaTypeId? baseTypeId,
        bool isAbstract,
        BinaryFacet binaryFacet,
        bool hasSimpleContent = false,
        string? simpleContentClrType = null,
        string? simpleContentXmlDataType = null,
        string? documentation = null)
    {
        Id = id;
        CSharpName = csharpName;
        Particle = particle;
        Attributes = attributes;
        BaseTypeId = baseTypeId;
        IsAbstract = isAbstract;
        BinaryFacet = binaryFacet;
        HasSimpleContent = hasSimpleContent;
        SimpleContentClrType = simpleContentClrType;
        SimpleContentXmlDataType = simpleContentXmlDataType;
        Documentation = documentation;
    }

    /// <summary>Type identity.</summary>
    public SchemaTypeId Id { get; }

    /// <summary>Suggested C# type name (local name, sanitized).</summary>
    public string CSharpName { get; }

    /// <summary>Content particle, if any.</summary>
    public Particle? Particle { get; }

    /// <summary>Attributes.</summary>
    public IReadOnlyList<AttributeDecl> Attributes { get; }

    /// <summary>Base type when restriction/extension.</summary>
    public SchemaTypeId? BaseTypeId { get; }

    /// <summary>Whether the type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Binary content tagging.</summary>
    public BinaryFacet BinaryFacet { get; }

    /// <summary>Whether the type has simple content (text + attributes).</summary>
    public bool HasSimpleContent { get; }

    /// <summary>CLR type for simple content text (e.g. <c>string</c>, <c>byte[]</c>).</summary>
    public string? SimpleContentClrType { get; }

    /// <summary>Optional <c>XmlText(DataType=…)</c> lexical hint (<c>date</c>, <c>time</c>, <c>dateTime</c>, …).</summary>
    public string? SimpleContentXmlDataType { get; }

    /// <summary>Summary documentation from XSD annotation (<c>ccts:Definition</c> or plain text).</summary>
    public string? Documentation { get; }
}
