using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>Result of an emit pass.</summary>
public sealed class EmitResult
{
    /// <summary>Creates an emit result.</summary>
    public EmitResult(IReadOnlyList<EmittedFile> files) => Files = files;

    /// <summary>Generated files (relative path + syntax).</summary>
    public IReadOnlyList<EmittedFile> Files { get; }
}
