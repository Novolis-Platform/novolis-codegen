using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>Options shared by emit profiles — mold type shape, namespaces, spines, and hooks.</summary>
public sealed class EmitOptions
{
    /// <summary>Root namespace for generated types.</summary>
    public required string RootNamespace { get; init; }

    /// <summary>
    /// Maps XML schema namespaces to C# namespaces under <see cref="RootNamespace"/>.
    /// Defaults to <see cref="DefaultNamespaceMapper"/> (schema-agnostic).
    /// Product hosts (e.g. UBL) supply a custom <see cref="INamespaceMapper"/>.
    /// </summary>
    public INamespaceMapper NamespaceMapper { get; init; } = new DefaultNamespaceMapper();

    /// <summary>When set, document root types implement this interface name.</summary>
    public string? DocumentRootInterfaceName { get; init; }

    /// <summary>CLR collection type for repeating particles (default <c>System.Collections.ObjectModel.Collection</c>).</summary>
    public string CollectionTypeName { get; init; } = "System.Collections.ObjectModel.Collection";

    /// <summary>Emit one file per type when true; otherwise a single compilation unit.</summary>
    public bool OneFilePerType { get; init; } = true;

    /// <summary>Optional filter: only emit these type ids (and their closure is caller's responsibility).</summary>
    public IReadOnlySet<SchemaTypeId>? IncludeTypeIds { get; init; }

    /// <summary>StripEmbedded policy for Base/Lean profiles.</summary>
    public StripEmbeddedPolicy StripEmbeddedPolicy { get; init; } = StripEmbeddedPolicy.MetadataOnly;

    /// <summary>
    /// When set with <see cref="SpineDocumentRootNames"/>, document-root Base interfaces extend this
    /// shared spine (property intersection of those roots).
    /// </summary>
    public string? SpineInterfaceName { get; init; }

    /// <summary>
    /// Document local names that participate in the shared spine (e.g. Invoice/CreditNote/Reminder).
    /// Required for spine emission when <see cref="SpineInterfaceName"/> is set.
    /// </summary>
    public IReadOnlySet<string>? SpineDocumentRootNames { get; init; }

    /// <summary>Obsolete alias for <see cref="SpineInterfaceName"/>.</summary>
    [Obsolete("Use SpineInterfaceName.")]
    public string? BillingSpineInterfaceName
    {
        get => SpineInterfaceName;
        init => SpineInterfaceName = value;
    }

    /// <summary>Optional post-emit hooks applied by <see cref="XsdCodegen.Emit"/>.</summary>
    public IReadOnlyList<IXsdEmitHook>? Hooks { get; init; }

    /// <summary>
    /// When true (default), files start with <c>#nullable enable</c> and optional particles/attributes
    /// are annotated with <c>?</c> (including choice alternatives and optional collections).
    /// </summary>
    public bool EnableNullable { get; init; } = true;
}
