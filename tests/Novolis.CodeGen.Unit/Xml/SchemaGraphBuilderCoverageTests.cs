using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Unit.Xml;

public sealed class SchemaGraphBuilderCoverageTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    [Test]
    public async Task TinySchema_HeaderAttributesAndChoiceParticles()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var header = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "HeaderType")];
        await Assert.That(header.Attributes.Any(a => a.Name == "currencyID" && a.IsRequired)).IsTrue();

        var doc = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "DocumentType")];
        await Assert.That(doc.Particle!.Children.Any(c => c.Kind == ParticleKind.Choice)).IsTrue();

        var line = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "LineType")];
        await Assert.That(line.Particle!.Children[0].MinOccurs).IsEqualTo(1);
        await Assert.That(line.Particle!.Children[1].MinOccurs).IsEqualTo(0);
    }

    [Test]
    public async Task BinaryObject_HasSimpleContentAndBinaryFacet()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var bin = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "BinaryObjectType")];
        await Assert.That(bin.HasSimpleContent).IsTrue();
        await Assert.That(bin.SimpleContentClrType).IsEqualTo("byte[]");
        await Assert.That(bin.BinaryFacet).IsEqualTo(BinaryFacet.BinaryObject);
        await Assert.That(bin.Attributes.Any(a => a.Name == "mimeCode")).IsTrue();
    }

    [Test]
    public async Task LoadFromFiles_EmptyWhitespaceRoot_Throws()
    {
        await Assert.That(() => SchemaSetLoader.LoadFromDirectory("   "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task DocumentRoots_WhenMarkUnreferencedFalse_UsesAllowListOnly()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles(
            [Path.Combine(FixturesDir, "tiny.xsd")],
            new SchemaGraphOptions
            {
                MarkUnreferencedAsDocumentRoots = false,
                DocumentRootLocalNames = new HashSet<string> { "Document" }
            });
        await Assert.That(graph.DocumentRoots.Count()).IsEqualTo(1);
        await Assert.That(graph.DocumentRoots.Single().Name).IsEqualTo("Document");
    }

    [Test]
    public async Task TypeIdSequence_IncludesComplexAndSimple()
    {
        var graph = SchemaGraphBuilder.BuildFromDirectory(FixturesDir);
        await Assert.That(graph.TypeIdSequence.Count).IsEqualTo(graph.ComplexTypes.Count + graph.SimpleTypes.Count);
        await Assert.That(graph.Elements.Any(e => e.QualifiedName.Contains("Document", StringComparison.Ordinal))).IsTrue();
    }
}
