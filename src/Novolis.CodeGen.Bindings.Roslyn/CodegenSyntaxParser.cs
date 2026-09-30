using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Parses generated C# source into Roslyn syntax trees.</summary>
public static class CodegenSyntaxParser
{
    /// <summary>Parses generated source text into a <see cref="CompilationUnitSyntax"/>.</summary>
    /// <param name="source">Generated C# source.</param>
    /// <returns>Root compilation unit.</returns>
    public static CompilationUnitSyntax ParseGenerated(string source) =>
        (CompilationUnitSyntax)Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source, path: "").GetRoot();
}
