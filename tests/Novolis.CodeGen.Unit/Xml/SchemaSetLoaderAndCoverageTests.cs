using System.Xml.Schema;
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

namespace Novolis.CodeGen.Unit.Xml;

public sealed class SchemaSetLoaderAndCoverageTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    private static string BadSchemasDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "BadSchemas");

    [Test]
    public async Task LoadFromDirectory_CompilesTinyFolder()
    {
        var set = SchemaSetLoader.LoadFromDirectory(FixturesDir);
        await Assert.That(set.IsCompiled).IsTrue();
        await Assert.That(set.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task LoadFromDirectory_MissingPath_Throws()
    {
        await Assert.That(() => SchemaSetLoader.LoadFromDirectory(Path.Combine(FixturesDir, "no-such-dir")))
            .ThrowsExactly<DirectoryNotFoundException>();
    }

    [Test]
    public async Task LoadFromFiles_Null_Throws()
    {
        await Assert.That(() => SchemaSetLoader.LoadFromFiles(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task LoadFromFiles_EmptyEnumerable_CompilesEmptySet()
    {
        var set = SchemaSetLoader.LoadFromFiles(Array.Empty<string>());
        await Assert.That(set.IsCompiled).IsTrue();
        await Assert.That(set.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LoadFromFiles_InvalidSchema_ThrowsXmlSchemaException()
    {
        var path = Path.Combine(BadSchemasDir, "invalid-compile.xsd");
        await Assert.That(() => SchemaSetLoader.LoadFromFiles([path]))
            .ThrowsExactly<XmlSchemaException>();
    }

    [Test]
    public async Task LoadFromFiles_InvalidRead_ThrowsXmlSchemaException()
    {
        var path = Path.Combine(BadSchemasDir, "invalid-read.xsd");
        await Assert.That(() => SchemaSetLoader.LoadFromFiles([path]))
            .ThrowsExactly<XmlSchemaException>();
    }

    [Test]
    public async Task OnValidationEvent_WarningDoesNotThrow_ErrorThrows()
    {
        var warningCtor = typeof(ValidationEventArgs)
            .GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var parms = warningCtor.GetParameters();

        object?[] BuildArgs(XmlSeverityType severity)
        {
            var args = new object?[parms.Length];
            for (var i = 0; i < parms.Length; i++)
            {
                var p = parms[i];
                if (p.ParameterType == typeof(XmlSeverityType))
                    args[i] = severity;
                else if (p.ParameterType == typeof(string))
                    args[i] = severity == XmlSeverityType.Error ? "err" : "warn";
                else if (p.ParameterType == typeof(Exception) || p.ParameterType == typeof(XmlSchemaException))
                    args[i] = severity == XmlSeverityType.Error ? new XmlSchemaException("err") : null;
                else if (p.ParameterType == typeof(XmlSchemaObject))
                    args[i] = null;
                else if (p.ParameterType == typeof(object))
                    args[i] = null;
                else
                    args[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
            }

            return args;
        }

        SchemaSetLoader.OnValidationEvent(null, (ValidationEventArgs)warningCtor.Invoke(BuildArgs(XmlSeverityType.Warning)));
        await Assert.That(() => SchemaSetLoader.OnValidationEvent(null, (ValidationEventArgs)warningCtor.Invoke(BuildArgs(XmlSeverityType.Error))))
            .ThrowsExactly<XmlSchemaException>();
    }

    [Test]
    public async Task LoadFromFiles_WarningOnly_StillCompiles()
    {
        var path = Path.Combine(BadSchemasDir, "warning-read.xsd");
        var set = SchemaSetLoader.LoadFromFiles([path]);
        await Assert.That(set.IsCompiled).IsTrue();
    }

    [Test]
    public async Task SchemaTypeId_CompareAndToString()
    {
        var a = new SchemaTypeId("urn:a", "Z");
        var b = new SchemaTypeId("urn:a", "A");
        var c = new SchemaTypeId("", "Local");
        await Assert.That(a.CompareTo(b)).IsGreaterThan(0);
        await Assert.That(a.ToString()).IsEqualTo("{urn:a}Z");
        await Assert.That(c.ToString()).IsEqualTo("Local");
    }

    [Test]
    public async Task BuildFromDirectory_RespectsExcludedNamespaces()
    {
        var graph = SchemaGraphBuilder.BuildFromDirectory(FixturesDir, new SchemaGraphOptions
        {
            MarkUnreferencedAsDocumentRoots = true,
            DocumentRootLocalNames = new HashSet<string> { "Document", "A", "B" }
        });
        await Assert.That(graph.Elements.Any(e => e.IsDocumentRoot)).IsTrue();
        await Assert.That(graph.DocumentRoots.Any()).IsTrue();
    }

    [Test]
    public async Task SyntaxEmitWriter_WriteAll_CreatesFiles()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Coverage.Wire",
            OneFilePerType = true
        });
        var dir = Path.Combine(Path.GetTempPath(), "novolis-codegen-emit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var written = SyntaxEmitWriter.WriteAll(result, dir);
            await Assert.That(written.Count).IsGreaterThan(0);
            await Assert.That(File.Exists(written[0])).IsTrue();
            await Assert.That(File.ReadAllText(written[0]).Length).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task SyntaxEmitWriter_WriteAll_NullResultOrBlankDir_Throws()
    {
        await Assert.That(() => SyntaxEmitWriter.WriteAll(null!, Path.GetTempPath()))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => SyntaxEmitWriter.WriteAll(new EmitResult([]), "  "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task DefaultNamespaceMapper_UsesLastUriSegment()
    {
        var mapper = new DefaultNamespaceMapper();
        await Assert.That(mapper.Map("Root", "")).IsEqualTo("Root");
        await Assert.That(mapper.Map("Root", "urn:novolis:codegen:tiny"))
            .IsEqualTo("Root.Tiny");
        await Assert.That(mapper.Map("Root", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"))
            .IsEqualTo("Root.2");
        await Assert.That(mapper.Map("Root", "___"))
            .IsEqualTo("Root.Generated");
        await Assert.That(SchemaNamespaceMapper.Map("Root", "urn:novolis:codegen:tiny"))
            .IsEqualTo("Root.Tiny");
    }

    [Test]
    public async Task DefaultNamespaceMapper_BlankRoot_Throws()
    {
        await Assert.That(() => new DefaultNamespaceMapper().Map("  ", "urn:x"))
            .ThrowsExactly<ArgumentException>();
    }
}
