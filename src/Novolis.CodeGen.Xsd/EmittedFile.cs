using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

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
