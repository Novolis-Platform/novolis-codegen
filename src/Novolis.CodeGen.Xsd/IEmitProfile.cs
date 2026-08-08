using Microsoft.CodeAnalysis;
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

/// <summary>Options shared by emit profiles.</summary>
public sealed class EmitOptions
{
    /// <summary>Root namespace for generated types.</summary>
    public required string RootNamespace { get; init; }

    /// <summary>When set, document root types implement this interface name (e.g. <c>IUblDocument</c>).</summary>
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
    /// When set, document-root Base interfaces extend this shared spine interface
    /// (property intersection of all document roots in the emit set).
    /// </summary>
    public string? BillingSpineInterfaceName { get; init; }
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
