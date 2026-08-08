using System.Xml;
using System.Xml.Schema;

namespace Novolis.CodeGen.Xml;

/// <summary>Loads and compiles an <see cref="XmlSchemaSet"/> from XSD files.</summary>
public static class SchemaSetLoader
{
    /// <summary>
    /// Loads all <c>*.xsd</c> under <paramref name="schemaRoot"/>. Clears <c>schemaLocation</c> on includes
    /// before compile so imports are satisfied from schemas already in the set (UBL maindoc/common layout).
    /// </summary>
    public static XmlSchemaSet LoadFromDirectory(string schemaRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaRoot);
        if (!Directory.Exists(schemaRoot))
            throw new DirectoryNotFoundException(schemaRoot);

        var paths = Directory.EnumerateFiles(schemaRoot, "*.xsd", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return LoadFromFiles(paths);
    }

    /// <summary>Loads the given XSD file paths into a compiled schema set.</summary>
    public static XmlSchemaSet LoadFromFiles(IEnumerable<string> schemaFiles)
    {
        ArgumentNullException.ThrowIfNull(schemaFiles);
        var schemaSet = new XmlSchemaSet();

        foreach (var path in schemaFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new XmlUrlResolver()
            });
            var schema = XmlSchema.Read(reader, OnValidationEvent)
                         ?? throw new InvalidOperationException($"Failed to read schema '{path}'.");
            schema.SourceUri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
            schemaSet.Add(schema);
        }

        foreach (XmlSchema schema in schemaSet.Schemas())
        {
            foreach (XmlSchemaExternal include in schema.Includes)
                include.SchemaLocation = null;
        }

        schemaSet.XmlResolver = null;
        schemaSet.ValidationEventHandler += OnValidationEvent;
        schemaSet.Compile();
        return schemaSet;
    }

    /// <summary>Validation callback used while reading/compiling schemas.</summary>
    public static void OnValidationEvent(object? sender, ValidationEventArgs e)
    {
        if (e.Severity != XmlSeverityType.Error)
            return;

        throw new XmlSchemaException(e.Message, e.Exception);
    }
}
