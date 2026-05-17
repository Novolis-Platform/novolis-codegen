using System.Text.RegularExpressions;

namespace Novolis.CodeGen.Reflection.Dump;

internal static partial class DumpHelper
{
    public static string CleanId(string id) => NonLetterDigitUnderscoreRegex().Replace(id, "");

    public static string GetIndent() => new string(' ', 4);
    
    public static string GetIndent(int count) => new string(' ', count * 4);

    public static string ReplaceVarDeclaration<T>(string code) => VarDeclarationRegex().Replace(code, "return ");

    [GeneratedRegex(@"var\s+\w+\s*=\s*")]
    private static partial Regex VarDeclarationRegex();
    
    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex NonLetterDigitUnderscoreRegex();
}