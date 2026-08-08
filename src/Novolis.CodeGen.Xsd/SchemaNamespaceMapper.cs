namespace Novolis.CodeGen.Xsd;

/// <summary>
/// Convenience static entry for namespace mapping. Defaults to <see cref="DefaultNamespaceMapper"/>
/// (no UBL/Peppol hard-coding). Prefer injecting <see cref="INamespaceMapper"/> via <see cref="EmitOptions"/>.
/// </summary>
public static class SchemaNamespaceMapper
{
    /// <summary>Default schema-agnostic mapper.</summary>
    public static INamespaceMapper Default { get; } = new DefaultNamespaceMapper();

    /// <summary>Maps an XML namespace URI to a C# namespace under <paramref name="rootNamespace"/>.</summary>
    public static string Map(string rootNamespace, string xmlSchemaNamespace) =>
        Default.Map(rootNamespace, xmlSchemaNamespace);
}
