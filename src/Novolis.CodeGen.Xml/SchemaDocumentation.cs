using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Novolis.CodeGen.Xml;

/// <summary>Extracts a single summary string from XSD annotations (CCTS Definition preferred).</summary>
public static class SchemaDocumentation
{
    /// <summary>
    /// Reads documentation from an annotated schema object.
    /// Prefers <c>ccts:Definition</c> (any namespace local name <c>Definition</c>); otherwise plain documentation text.
    /// </summary>
    public static string? Extract(XmlSchemaAnnotated? annotated)
    {
        if (annotated?.Annotation is null)
            return null;

        foreach (var item in annotated.Annotation.Items)
        {
            if (item is not XmlSchemaDocumentation doc)
                continue;

            var fromMarkup = ExtractFromMarkup(doc.Markup);
            if (!string.IsNullOrWhiteSpace(fromMarkup))
                return Normalize(fromMarkup);
        }

        return null;
    }

    private static string? ExtractFromMarkup(XmlNode?[]? markup)
    {
        if (markup is null || markup.Length == 0)
            return null;

        string? definition = null;
        var plain = new StringBuilder();

        foreach (var node in markup)
        {
            if (node is null)
                continue;
            if (node is XmlElement el)
            {
                if (string.Equals(el.LocalName, "Definition", StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(el.InnerText))
                {
                    definition = el.InnerText;
                }

                // Nested CCTS Component: look one level for Definition.
                if (definition is null)
                {
                    foreach (XmlNode child in el.ChildNodes)
                    {
                        if (child is XmlElement childEl
                            && string.Equals(childEl.LocalName, "Definition", StringComparison.Ordinal)
                            && !string.IsNullOrWhiteSpace(childEl.InnerText))
                        {
                            definition = childEl.InnerText;
                            break;
                        }
                    }
                }

                continue;
            }

            if (node.NodeType is XmlNodeType.Text or XmlNodeType.CDATA
                && !string.IsNullOrWhiteSpace(node.Value))
            {
                if (plain.Length > 0)
                    plain.Append(' ');
                plain.Append(node.Value.Trim());
            }
        }

        return definition ?? (plain.Length > 0 ? plain.ToString() : null);
    }

    private static string Normalize(string text)
    {
        var collapsed = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Length == 0 ? string.Empty : collapsed;
    }
}
