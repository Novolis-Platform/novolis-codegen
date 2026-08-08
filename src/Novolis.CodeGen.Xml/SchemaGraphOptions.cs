using System.Xml;
using System.Xml.Schema;

namespace Novolis.CodeGen.Xml;

/// <summary>Options for building a <see cref="SchemaGraph"/>.</summary>
public sealed class SchemaGraphOptions
{
    /// <summary>
    /// When true, global elements that are never referenced as particles are marked document roots.
    /// When false, only elements whose local name matches <see cref="DocumentRootLocalNames"/> are roots.
    /// </summary>
    public bool MarkUnreferencedAsDocumentRoots { get; init; } = true;

    /// <summary>Optional allow-list of document root local names (case-sensitive).</summary>
    public IReadOnlySet<string>? DocumentRootLocalNames { get; init; }

    /// <summary>Namespace URIs to exclude entirely from the graph.</summary>
    public IReadOnlySet<string> ExcludedNamespaces { get; init; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "http://www.w3.org/2000/09/xmldsig#",
            "http://uri.etsi.org/01903/v1.3.2#",
            "http://uri.etsi.org/01903/v1.4.1#"
        };
}
