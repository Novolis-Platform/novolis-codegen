using System.Text.RegularExpressions;

namespace Novolis.CodeGen.Xsd;

/// <summary>
/// Schema-agnostic namespace mapper: empty XML namespace stays at root;
/// otherwise uses the last sanitised URI segment (no product-specific hard-coding).
/// </summary>
public sealed class DefaultNamespaceMapper : INamespaceMapper
{
    /// <inheritdoc />
    public string Map(string rootNamespace, string xmlSchemaNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootNamespace);
        if (string.IsNullOrEmpty(xmlSchemaNamespace))
            return rootNamespace;

        var customNamespace = Regex.Replace(xmlSchemaNamespace, "[^a-zA-Z0-9]", "_");
        var last = customNamespace.Split('_', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrEmpty(last))
            return rootNamespace + ".Generated";

        var suffix = char.ToUpperInvariant(last[0]) + last[1..];
        return rootNamespace + "." + suffix;
    }
}
