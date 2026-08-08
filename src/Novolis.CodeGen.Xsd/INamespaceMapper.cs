namespace Novolis.CodeGen.Xsd;

/// <summary>Maps XML schema target namespaces to C# namespaces under a root.</summary>
public interface INamespaceMapper
{
    /// <summary>Maps an XML namespace URI to a C# namespace under <paramref name="rootNamespace"/>.</summary>
    string Map(string rootNamespace, string xmlSchemaNamespace);
}
