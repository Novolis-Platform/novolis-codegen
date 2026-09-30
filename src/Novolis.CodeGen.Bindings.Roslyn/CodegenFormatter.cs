using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

internal static class CodegenFormatter
{
    public static string FormatCompilationUnit(CompilationUnitSyntax unit)
    {
        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var formatted = Microsoft.CodeAnalysis.Formatting.Formatter.Format(unit, workspace);
        return formatted.NormalizeWhitespace(eol: "\n").ToFullString();
    }
}
