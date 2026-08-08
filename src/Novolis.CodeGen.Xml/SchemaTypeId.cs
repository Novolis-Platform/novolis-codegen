namespace Novolis.CodeGen.Xml;

/// <summary>Stable identity for a schema type (namespace + local name).</summary>
public readonly record struct SchemaTypeId(string NamespaceName, string LocalName) : IComparable<SchemaTypeId>
{
    /// <inheritdoc />
    public int CompareTo(SchemaTypeId other)
    {
        var ns = string.CompareOrdinal(NamespaceName, other.NamespaceName);
        return ns != 0 ? ns : string.CompareOrdinal(LocalName, other.LocalName);
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.IsNullOrEmpty(NamespaceName) ? LocalName : $"{{{NamespaceName}}}{LocalName}";
}
