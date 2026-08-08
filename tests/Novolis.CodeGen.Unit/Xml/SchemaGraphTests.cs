using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Unit.Xml;

public sealed class SchemaGraphTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    [Test]
    public async Task LoadTinySchema_ProducesNonEmptyGraph()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        await Assert.That(graph.ComplexTypes.Count).IsGreaterThan(0);
        await Assert.That(graph.Elements.Count).IsGreaterThan(0);
        await Assert.That(graph.DocumentRoots.Any(e => e.Name == "Document")).IsTrue();
    }

    [Test]
    public async Task ComplexSequence_PreservesOrderAndCardinality()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var doc = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "DocumentType")];
        await Assert.That(doc.Particle).IsNotNull();
        var seq = doc.Particle!;
        await Assert.That(seq.Kind).IsEqualTo(ParticleKind.Sequence);
        await Assert.That(seq.Children.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(seq.Children[0].ElementName).IsEqualTo("Header");
        await Assert.That(seq.Children[1].ElementName).IsEqualTo("Line");
        await Assert.That(seq.Children[1].IsUnbounded).IsTrue();
    }

    [Test]
    public async Task BinaryFacetTagged_OnBase64AndBinaryObject()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var payload = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:tiny", "BinaryPayloadType")];
        await Assert.That(payload.BinaryFacet).IsEqualTo(BinaryFacet.Base64Binary);

        var binObj = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "BinaryObjectType")];
        await Assert.That(binObj.BinaryFacet).IsEqualTo(BinaryFacet.BinaryObject);
    }

    [Test]
    public async Task DedupeIncludes_SharedImportHasSingleTypeId()
    {
        var dir = FixturesDir;
        var graph = SchemaGraphBuilder.BuildFromFiles(
        [
            Path.Combine(dir, "common.xsd"),
            Path.Combine(dir, "doc-a.xsd"),
            Path.Combine(dir, "doc-b.xsd")
        ]);

        var sharedId = new SchemaTypeId("urn:novolis:codegen:common", "SharedType");
        await Assert.That(graph.ComplexById.ContainsKey(sharedId)).IsTrue();
        await Assert.That(graph.ComplexTypes.Count(t => t.Id.Equals(sharedId))).IsEqualTo(1);
    }

    [Test]
    public async Task DeterministicOrder_TwoBuildsMatchTypeIdSequence()
    {
        var path = Path.Combine(FixturesDir, "tiny.xsd");
        var a = SchemaGraphBuilder.BuildFromFiles([path]).TypeIdSequence;
        var b = SchemaGraphBuilder.BuildFromFiles([path]).TypeIdSequence;
        await Assert.That(a.SequenceEqual(b)).IsTrue();
    }
}
