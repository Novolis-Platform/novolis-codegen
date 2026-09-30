using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>How emitted Roslyn syntax is formatted before writing to disk.</summary>
public enum FormatPolicy
{
    /// <summary>Use the Roslyn workspace formatter.</summary>
    RoslynFormatter,

    /// <summary>Normalize whitespace only (faster, less opinionated).</summary>
    NormalizeWhitespace,
}
