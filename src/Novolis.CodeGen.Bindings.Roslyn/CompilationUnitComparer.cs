using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

public static class CompilationUnitComparer
{
    public static bool AreStructurallyEquivalent(string committedSource, string emittedSource)
    {
        var committed = CodegenSyntaxParser.ParseGenerated(committedSource);
        var emitted = CodegenSyntaxParser.ParseGenerated(emittedSource);
        return AreStructurallyEquivalent(committed, emitted);
    }

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
