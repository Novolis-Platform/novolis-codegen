using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

namespace Novolis.CodeGen.Unit.Xml;

public sealed class SchemaSetLoaderAndCoverageTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

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
    public async Task SchemaNamespaceMapper_MapsUblInvoice()
    {
        var ns = SchemaNamespaceMapper.Map("Novolis.Xsd.Ubl", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        await Assert.That(ns).IsEqualTo("Novolis.Xsd.Ubl.Invoice");
    }
}
