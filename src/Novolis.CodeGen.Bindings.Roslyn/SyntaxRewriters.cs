using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Small Roslyn rewriters used by codegen hooks.</summary>
public static class SyntaxRewriters
{
    /// <summary>Ensures a using directive for <paramref name="namespaceName"/> is present.</summary>
    /// <param name="unit">Compilation unit.</param>
    /// <param name="namespaceName">Namespace to import.</param>
    /// <returns>Updated compilation unit.</returns>
    public static CompilationUnitSyntax EnsureUsing(CompilationUnitSyntax unit, string namespaceName)
    {
        if (unit.Usings.Any(u => u.Name?.ToString() == namespaceName))
            return unit;

        var usingDirective = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.UsingDirective(
            Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseName(namespaceName));
        return unit.WithUsings(unit.Usings.Add(usingDirective));
    }
}
