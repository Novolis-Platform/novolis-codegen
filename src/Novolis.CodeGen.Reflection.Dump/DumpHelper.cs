using System.Text.RegularExpressions;

namespace Novolis.CodeGen.Reflection.Dump;

internal static partial class DumpHelper
{
    public static string CleanId(string id) => NonLetterDigitUnderscoreRegex().Replace(id, "");

    /// <summary>File-name safe id: letters, digits, underscore, and hyphen.</summary>
    public static string ToFileSafeId(string id)
    {
        var safe = FileSafeIdRegex().Replace(id, "");
        if (string.IsNullOrEmpty(safe))
            throw new ArgumentException("Id must contain at least one letter, digit, underscore, or hyphen.", nameof(id));
        return safe;
    }

    public static string GetIndent() => new string(' ', 4);
    
    public static string GetIndent(int count) => new string(' ', count * 4);

    public static string ReplaceVarDeclaration<T>(string code) => VarDeclarationRegex().Replace(code, "return ");

    [GeneratedRegex(@"var\s+\w+\s*=\s*")]
    private static partial Regex VarDeclarationRegex();
    
    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex NonLetterDigitUnderscoreRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9_-]")]
    private static partial Regex FileSafeIdRegex();
}