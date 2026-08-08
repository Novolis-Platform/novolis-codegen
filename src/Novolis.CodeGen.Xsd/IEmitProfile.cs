using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>Emits C# syntax from a <see cref="SchemaGraph"/>.</summary>
public interface IEmitProfile
{
    /// <summary>Profile name for logging.</summary>
    string Name { get; }

    /// <summary>Emits one compilation unit (or multiple files via <see cref="EmitResult"/>).</summary>
    EmitResult Emit(SchemaGraph graph, EmitOptions options);
}

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
}

/// <summary>How binary embeddings are handled in Base/Lean emit.</summary>
public enum StripEmbeddedPolicy
{
    /// <summary>Omit byte[] Value; keep mime/filename/uri metadata as <c>BinaryObjectRef</c>.</summary>
    MetadataOnly = 0,

    /// <summary>Omit binary properties entirely.</summary>
    Omit = 1
}

/// <summary>Result of an emit pass.</summary>
public sealed class EmitResult
{
    /// <summary>Creates an emit result.</summary>
    public EmitResult(IReadOnlyList<EmittedFile> files) => Files = files;

    /// <summary>Generated files (relative path + syntax).</summary>
    public IReadOnlyList<EmittedFile> Files { get; }
}

/// <summary>A single emitted source file.</summary>
public sealed class EmittedFile
{
    /// <summary>Creates an emitted file.</summary>
    public EmittedFile(string relativePath, CompilationUnitSyntax compilationUnit)
    {
        RelativePath = relativePath;
        CompilationUnit = compilationUnit;
    }

    /// <summary>Relative path under the output directory.</summary>
    public string RelativePath { get; }

    /// <summary>Compilation unit syntax.</summary>
    public CompilationUnitSyntax CompilationUnit { get; }
}
