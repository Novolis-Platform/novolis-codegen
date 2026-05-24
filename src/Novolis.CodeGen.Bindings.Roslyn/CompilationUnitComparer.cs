using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Compares generated and committed Roslyn compilation units for structural equivalence.</summary>
public static class CompilationUnitComparer
{
    /// <summary>
    /// Returns whether committed and emitted source are structurally equivalent (ignoring insignificant whitespace).
    /// </summary>
    /// <param name="committedSource">Source currently in the repository.</param>
    /// <param name="emittedSource">Freshly generated source.</param>
    /// <returns><see langword="true"/> when equivalent.</returns>
    public static bool AreStructurallyEquivalent(string committedSource, string emittedSource)
    {
        var committed = CodegenSyntaxParser.ParseGenerated(committedSource);
        var emitted = CodegenSyntaxParser.ParseGenerated(emittedSource);
        return AreStructurallyEquivalent(committed, emitted);
    }

    /// <summary>
    /// Returns whether two compilation units are structurally equivalent (member count and normalized member text).
    /// </summary>
    /// <param name="committed">Committed compilation unit.</param>
    /// <param name="emitted">Emitted compilation unit.</param>
    /// <returns><see langword="true"/> when equivalent.</returns>
    public static bool AreStructurallyEquivalent(CompilationUnitSyntax committed, CompilationUnitSyntax emitted)
    {
        var committedMembers = committed.Members.Select(NormalizeMember).ToList();
        var emittedMembers = emitted.Members.Select(NormalizeMember).ToList();
        if (committedMembers.Count != emittedMembers.Count)
            return false;

        for (var i = 0; i < committedMembers.Count; i++)
        {
            if (!string.Equals(committedMembers[i], emittedMembers[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static string NormalizeMember(MemberDeclarationSyntax member)
    {
        var normalized = member.NormalizeWhitespace(eol: "\n").ToFullString();
        return CollapseWhitespace(normalized);
    }

    private static string CollapseWhitespace(string text)
    {
        var lines = text.Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrWhiteSpace(l) || l.Length == 0);
        return string.Join('\n', lines).Trim();
    }
}
