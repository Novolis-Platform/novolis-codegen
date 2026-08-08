using System.Text.RegularExpressions;

namespace Novolis.CodeGen.Xsd;

/// <summary>Maps XML schema target namespaces to C# namespaces under a root.</summary>
public static class SchemaNamespaceMapper
{
    /// <summary>Maps an XML namespace URI to a C# namespace under <paramref name="rootNamespace"/>.</summary>
    public static string Map(string rootNamespace, string xmlSchemaNamespace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootNamespace);
        if (string.IsNullOrEmpty(xmlSchemaNamespace))
            return rootNamespace;

        var customNamespace = Regex.Replace(xmlSchemaNamespace, "[^a-zA-Z0-9]", "_");
        var suffix = ResolveSuffix(customNamespace);
        return string.IsNullOrEmpty(suffix) ? rootNamespace : rootNamespace + "." + suffix;
    }

    private static string ResolveSuffix(string customNamespace)
    {
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_CommonExtensionComponents_2") return "CommonExtensionComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_CommonSignatureComponents_2") return "CommonSignatureComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_SignatureBasicComponents_2") return "SignatureBasicComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_SignatureAggregateComponents_2") return "SignatureAggregateComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_CommonAggregateComponents_2") return "CommonAggregateComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_CommonBasicComponents_2") return "CommonBasicComponents";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_UnqualifiedDataTypes_2") return "UnqualifiedDataTypes";
        if (customNamespace == "urn_oasis_names_specification_ubl_schema_xsd_BaseDocument_2") return "BaseDocument";

        if (customNamespace.StartsWith("urn_oasis_names_specification_ubl_schema_xsd_", StringComparison.Ordinal))
        {
            return customNamespace
                .Replace("urn_oasis_names_specification_ubl_schema_xsd_", "", StringComparison.Ordinal)
                .Replace("_2", "", StringComparison.Ordinal);
        }

        if (customNamespace == "urn_un_unece_uncefact_data_specification_CoreComponentTypeSchemaModule_2") return "CoreComponentTypes";
        if (customNamespace.StartsWith("http___www_w3_org_2000_09_xmldsig", StringComparison.Ordinal)) return "XmlDsig";
        if (customNamespace.StartsWith("http___uri_etsi_org_01903_", StringComparison.Ordinal)) return "Xades";
        // SBDH / Peppol envelope types live at the package root (Novolis.Xsd.Peppol), not .Envelope.
        if (customNamespace.Contains("StandardBusinessDocumentHeader", StringComparison.Ordinal)) return string.Empty;

        // Synthetic / fixture namespaces: last segment after colon or slash
        var last = customNamespace.Split('_', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrEmpty(last) ? "Generated" : char.ToUpperInvariant(last[0]) + last[1..];
    }
}
