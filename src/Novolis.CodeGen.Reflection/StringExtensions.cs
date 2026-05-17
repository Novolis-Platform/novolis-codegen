namespace Novolis.CodeGen.Reflection;

internal static class StringExtensions
{
    /// <summary>
    /// Retrieves the first token of a string by splitting it at the specified character.
    /// </summary>
    /// <param name="s">The string to split.</param>
    /// <param name="splitChar">The character used to split the string.</param>
    /// <returns>The first token of the string or the whole string if the split character is not found.</returns>
    public static string FirstToken(this string s, char splitChar)
    {
        var idx = s.IndexOf(splitChar);
        return idx != -1 ? s.Substring(0, idx) : s;
    }

    /// <summary>
    /// Returns the last token in a string, given a split character.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="splitChar"></param>
    /// <returns></returns>
    public static string LastToken(this string s, char splitChar)
    {
        var idx = s.LastIndexOf(splitChar);
        return idx != -1 ? s.Substring(idx + 1) : s;
    }
}