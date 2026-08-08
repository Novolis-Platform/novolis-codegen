namespace Novolis.CodeGen.Xml;

/// <summary>Attribute declaration on a complex type.</summary>
public sealed class AttributeDecl
{
    /// <summary>Creates an attribute declaration.</summary>
    public AttributeDecl(
        string name,
        string? namespaceName,
        SchemaTypeId? typeId,
        bool isRequired,
        string? defaultValue = null)
    {
        Name = name;
        NamespaceName = namespaceName;
        TypeId = typeId;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }

    /// <summary>Local attribute name.</summary>
    public string Name { get; }

    /// <summary>Attribute namespace URI (null for no-namespace / unqualified).</summary>
    public string? NamespaceName { get; }

    /// <summary>Attribute type identity when known.</summary>
    public SchemaTypeId? TypeId { get; }

    /// <summary>Whether use="required".</summary>
    public bool IsRequired { get; }

    /// <summary>Default value if any.</summary>
    public string? DefaultValue { get; }
}
