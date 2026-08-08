using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Novolis.CodeGen.Xsd;

/// <summary>Shared nullable-reference helpers for emit profiles.</summary>
internal static class EmitNullability
{
    /// <summary><c>#nullable enable</c> leading trivia for generated compilation units.</summary>
    public static SyntaxTriviaList EnableDirective() =>
        TriviaList(
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
            EndOfLine("\n"));

    /// <summary>
    /// Annotates a CLR type for schema optionality.
    /// Collections become <c>Collection&lt;T&gt;?</c> when optional; scalars get a trailing <c>?</c>.
    /// </summary>
    public static string Annotate(string clrType, bool optional, bool collection, string? collectionTypeName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clrType);
        if (collection)
        {
            var coll = string.IsNullOrWhiteSpace(collectionTypeName)
                ? "Collection"
                : ShortCollectionName(collectionTypeName);
            var open = $"{coll}<{StripNullableSuffix(clrType)}>";
            return optional ? open + "?" : open;
        }

        if (!optional)
            return clrType;

        return clrType.EndsWith('?') ? clrType : clrType + "?";
    }

    /// <summary>Parses <see cref="Annotate"/> into a <see cref="TypeSyntax"/>.</summary>
    public static TypeSyntax ParseAnnotated(string clrType, bool optional, bool collection, string? collectionTypeName = null) =>
        ParseTypeName(Annotate(clrType, optional, collection, collectionTypeName));

    private static string StripNullableSuffix(string clrType) =>
        clrType.EndsWith('?') ? clrType[..^1] : clrType;

    private static string ShortCollectionName(string collectionTypeName)
    {
        var lastDot = collectionTypeName.LastIndexOf('.');
        return lastDot >= 0 ? collectionTypeName[(lastDot + 1)..] : collectionTypeName;
    }
}
