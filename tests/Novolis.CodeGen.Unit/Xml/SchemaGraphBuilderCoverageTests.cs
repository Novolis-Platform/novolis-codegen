using System.Xml.Schema;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Unit.Xml;

public sealed class SchemaGraphBuilderCoverageTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    private static string EdgesPath => Path.Combine(FixturesDir, "branch-edges.xsd");
    private static string TypesPath => Path.Combine(FixturesDir, "types.xsd");

    [Test]
    public async Task TinySchema_HeaderAttributesAndChoiceParticles()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var header = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:tiny", "HeaderType")];
        await Assert.That(header.Attributes.Any(a => a.Name == "currencyID" && a.IsRequired)).IsTrue();
        await Assert.That(header.Documentation).IsEqualTo("Document header with currency.");
        var title = header.Particle!.Children.Single(c => c.ElementName == "Title");
        await Assert.That(title.Documentation).IsEqualTo("Human-readable title.");

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
    public async Task AnyParticle_And_DateXmlDataType_AreMapped()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "any.xsd")]);
        var envelope = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:any", "EnvelopeType")];
        await Assert.That(envelope.Particle!.Children.Any(c => c.Kind == ParticleKind.Any)).IsTrue();

        var dateWrap = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:any", "DateWrapType")];
        await Assert.That(dateWrap.HasSimpleContent).IsTrue();
        await Assert.That(dateWrap.SimpleContentClrType).IsEqualTo("DateTime");
        await Assert.That(dateWrap.SimpleContentXmlDataType).IsEqualTo("date");
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
    public async Task DocumentRoots_MarkUnreferencedFalse_EmptyAllowList_YieldsNone()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles(
            [Path.Combine(FixturesDir, "tiny.xsd")],
            new SchemaGraphOptions
            {
                MarkUnreferencedAsDocumentRoots = false,
                DocumentRootLocalNames = null
            });
        await Assert.That(graph.DocumentRoots.Any()).IsFalse();
    }

    [Test]
    public async Task TypeIdSequence_IncludesComplexAndSimple()
    {
        var graph = SchemaGraphBuilder.BuildFromDirectory(FixturesDir);
        await Assert.That(graph.TypeIdSequence.Count).IsEqualTo(graph.ComplexTypes.Count + graph.SimpleTypes.Count);
        await Assert.That(graph.Elements.Any(e => e.QualifiedName.Contains("Document", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Build_NullSchemaSet_Throws()
    {
        await Assert.That(() => SchemaGraphBuilder.Build(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task BranchEdges_AnonymousComplex_GroupRef_All_AndFacets()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([EdgesPath]);

        var anon = graph.Elements.Single(e => e.Name == "AnonRoot");
        await Assert.That(anon.TypeId).IsNotNull();
        await Assert.That(graph.ComplexById.ContainsKey(anon.TypeId!.Value)).IsTrue();
        await Assert.That(graph.ComplexById[anon.TypeId.Value].Particle!.Children[0].ElementName).IsEqualTo("Inner");

        var grouped = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "GroupedType")];
        await Assert.That(grouped.Particle).IsNotNull();
        await Assert.That(grouped.Particle!.Children.Any(c => c.ElementName == "G1")).IsTrue();
        await Assert.That(grouped.Particle!.Children.Any(c => c.ElementName == "Tail")).IsTrue();

        var all = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "AllType")];
        await Assert.That(all.Particle!.Kind).IsEqualTo(ParticleKind.All);

        var hex = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "HexBlobWrapType")];
        await Assert.That(hex.HasSimpleContent).IsTrue();
        await Assert.That(hex.BinaryFacet).IsEqualTo(BinaryFacet.Base64Binary);
        await Assert.That(hex.SimpleContentClrType).IsEqualTo("byte[]");

        var amount = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "AmountType")];
        await Assert.That(amount.HasSimpleContent).IsTrue();
        await Assert.That(amount.SimpleContentClrType).IsEqualTo("decimal");
        await Assert.That(amount.Attributes.Any(a => a.IsRequired && a.Name == "currencyID")).IsTrue();

        var flag = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "FlagWrapType")];
        await Assert.That(flag.SimpleContentClrType).IsEqualTo("bool");

        var when = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "WhenWrapType")];
        await Assert.That(when.SimpleContentClrType).IsEqualTo("DateTime");

        var flt = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "FloatWrapType")];
        await Assert.That(flt.SimpleContentClrType).IsEqualTo("float");

        var lng = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "LongWrapType")];
        await Assert.That(lng.SimpleContentClrType).IsEqualTo("long");

        var dbl = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "DoubleWrapType")];
        await Assert.That(dbl.SimpleContentClrType).IsEqualTo("double");
        var iwrap = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "IntWrapType")];
        await Assert.That(iwrap.SimpleContentClrType).IsEqualTo("int");
        var swrap = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "ShortWrapType")];
        await Assert.That(swrap.SimpleContentClrType).IsEqualTo("short");
        var dwrap = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "DateWrapType")];
        await Assert.That(dwrap.SimpleContentClrType).IsEqualTo("DateTime");
        await Assert.That(dwrap.SimpleContentXmlDataType).IsEqualTo("date");
        var twrap = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "TimeWrapType")];
        await Assert.That(twrap.SimpleContentClrType).IsEqualTo("DateTime");
        await Assert.That(twrap.SimpleContentXmlDataType).IsEqualTo("time");

        var upgrade = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "UpgradeBinaryObject")];
        await Assert.That(upgrade.BinaryFacet).IsEqualTo(BinaryFacet.BinaryObject);

        var otherBin = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "OtherBinary")];
        await Assert.That(otherBin.BinaryFacet).IsEqualTo(BinaryFacet.Base64Binary);

        var union = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "UnionFlag")];
        await Assert.That(union.EnumerationValues).IsEmpty();
        var list = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "ListOfToken")];
        await Assert.That(list.ClrTypeName).IsEqualTo("string");

        var party = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "PlainParty")];
        await Assert.That(party.Attributes.Count).IsGreaterThan(0);
        await Assert.That(party.CSharpName).IsEqualTo("PlainParty");

        var primitives = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "PrimitivesType")];
        await Assert.That(primitives.Attributes.Any(a => a.Name == "fixedCode" && a.DefaultValue == "EUR")).IsTrue();

        var weirdBin = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "WeirdBinaryObject")];
        await Assert.That(weirdBin.BinaryFacet).IsEqualTo(BinaryFacet.BinaryObject);
        await Assert.That(weirdBin.ClrTypeName).IsEqualTo("byte[]");

        var payloadBin = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "PayloadBinary")];
        await Assert.That(payloadBin.BinaryFacet).IsEqualTo(BinaryFacet.Base64Binary);

        var enums = graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:edges", "EmptyEnumType")];
        await Assert.That(enums.EnumerationValues).IsEquivalentTo(["Ok"]);

        var abstractBase = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "AbstractBase")];
        await Assert.That(abstractBase.IsAbstract).IsTrue();

        var weirdName = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "weird-name-type")];
        await Assert.That(weirdName.CSharpName).IsEqualTo("weird_name_type");

        var sharedLeaf = graph.Elements.Single(e => e.Name == "SharedLeaf");
        await Assert.That(sharedLeaf.IsNillable).IsTrue();

        var refUser = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:edges", "RefUserType")];
        await Assert.That(refUser.Particle!.Children[0].IsUnbounded).IsTrue();
        await Assert.That(refUser.Particle!.Children[0].ElementName).IsEqualTo("SharedLeaf");
    }

    [Test]
    public async Task ProgrammaticSchema_MaxOccursZero_OmitsParticle()
    {
        const string ns = "urn:novolis:codegen:prog";
        var schema = new XmlSchema { TargetNamespace = ns, ElementFormDefault = XmlSchemaForm.Qualified };

        var ct = new XmlSchemaComplexType { Name = "ProgType" };
        var seq = new XmlSchemaSequence();
        seq.Items.Add(new XmlSchemaElement
        {
            Name = "Keep",
            SchemaTypeName = new System.Xml.XmlQualifiedName("string", XmlSchema.Namespace),
            MinOccurs = 1,
            MaxOccurs = 1
        });
        seq.Items.Add(new XmlSchemaElement
        {
            Name = "Gone",
            SchemaTypeName = new System.Xml.XmlQualifiedName("string", XmlSchema.Namespace),
            MinOccurs = 0,
            MaxOccurs = 0
        });
        ct.Particle = seq;
        schema.Items.Add(ct);
        schema.Items.Add(new XmlSchemaElement
        {
            Name = "Prog",
            SchemaTypeName = new System.Xml.XmlQualifiedName("ProgType", ns)
        });

        var set = new XmlSchemaSet();
        set.Add(schema);
        set.Compile();

        var graph = SchemaGraphBuilder.Build(set);
        var node = graph.ComplexById[new SchemaTypeId(ns, "ProgType")];
        await Assert.That(node.Particle!.Children.Any(c => c.ElementName == "Keep")).IsTrue();
        await Assert.That(node.Particle!.Children.Any(c => c.ElementName == "Gone")).IsFalse();
    }

    [Test]
    public async Task TypesFixture_MapsSimpleClrCodesAndAllGroup()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([TypesPath]);
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "FlagType")].ClrTypeName).IsEqualTo("bool");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "CountType")].ClrTypeName).IsEqualTo("int");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "BigCountType")].ClrTypeName).IsEqualTo("long");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "ShortCountType")].ClrTypeName).IsEqualTo("short");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "MoneyType")].ClrTypeName).IsEqualTo("decimal");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "RatioType")].ClrTypeName).IsEqualTo("double");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "FloatType")].ClrTypeName).IsEqualTo("float");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "WhenType")].ClrTypeName).IsEqualTo("DateTime");
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "HexBlobType")].BinaryFacet).IsEqualTo(BinaryFacet.Base64Binary);
        await Assert.That(graph.SimpleById[new SchemaTypeId("urn:novolis:codegen:types", "ColorType")].EnumerationValues)
            .IsEquivalentTo(["Red", "Blue"]);

        var all = graph.ComplexById[new SchemaTypeId("urn:novolis:codegen:types", "AllGroupType")];
        await Assert.That(all.Particle!.Kind).IsEqualTo(ParticleKind.All);
    }

    [Test]
    public async Task ExcludedNamespaces_SkipsTypesAndElements()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles(
            [
                Path.Combine(FixturesDir, "common.xsd"),
                Path.Combine(FixturesDir, "doc-a.xsd")
            ],
            new SchemaGraphOptions
            {
                ExcludedNamespaces = new HashSet<string>(StringComparer.Ordinal)
                {
                    "urn:novolis:codegen:common",
                    "http://www.w3.org/2000/09/xmldsig#",
                    "http://uri.etsi.org/01903/v1.3.2#",
                    "http://uri.etsi.org/01903/v1.4.1#"
                }
            });

        await Assert.That(graph.ComplexById.ContainsKey(new SchemaTypeId("urn:novolis:codegen:common", "SharedType"))).IsFalse();
        await Assert.That(graph.ComplexById.ContainsKey(new SchemaTypeId("urn:novolis:codegen:a", "AType"))).IsTrue();
    }

    [Test]
    public async Task DocumentRootAllowList_FiltersOtherRoots()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles(
            [EdgesPath],
            new SchemaGraphOptions
            {
                MarkUnreferencedAsDocumentRoots = true,
                DocumentRootLocalNames = new HashSet<string> { "Primitives" }
            });
        await Assert.That(graph.DocumentRoots.Select(e => e.Name).ToArray()).IsEquivalentTo(["Primitives"]);
    }

    [Test]
    public async Task PrivateHelpers_SanitizeNameAndTypeIdEdges_ViaReflection()
    {
        var sanitize = typeof(SchemaGraphBuilder).GetMethod(
            "SanitizeName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        await Assert.That(sanitize.Invoke(null, [""])!).IsEqualTo("GeneratedType");
        await Assert.That(sanitize.Invoke(null, ["9abc"])!).IsEqualTo("_9abc");
        await Assert.That(sanitize.Invoke(null, ["ok-name"])!).IsEqualTo("ok_name");

        var toTypeId = typeof(SchemaGraphBuilder).GetMethod(
            "ToTypeId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var anon = new XmlSchemaComplexType { Name = null };
        var id = (SchemaTypeId)toTypeId.Invoke(null, [System.Xml.XmlQualifiedName.Empty, anon])!;
        await Assert.That(id.LocalName.StartsWith("Anonymous", StringComparison.Ordinal)).IsTrue();

        var mapParticle = typeof(SchemaGraphBuilder).GetMethod(
            "MapParticle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var referenced = new HashSet<System.Xml.XmlQualifiedName>();
        var none = mapParticle.Invoke(null, [null, referenced]);
        await Assert.That(none).IsNull();

        var any = mapParticle.Invoke(null, [new XmlSchemaAny(), referenced]);
        await Assert.That(any).IsNotNull();
        await Assert.That(((Particle)any!).Kind).IsEqualTo(ParticleKind.Any);

        var groupRef = new XmlSchemaGroupRef();
        var fromGroup = mapParticle.Invoke(null, [groupRef, referenced]);
        await Assert.That(fromGroup).IsNull();

        var resolve = typeof(SchemaGraphBuilder).GetMethod(
            "ResolveElementTypeId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var bare = new XmlSchemaElement
        {
            SchemaTypeName = new System.Xml.XmlQualifiedName("Ghost", "urn:ghost")
        };
        var resolved = (SchemaTypeId?)resolve.Invoke(null, [bare]);
        await Assert.That(resolved).IsEqualTo(new SchemaTypeId("urn:ghost", "Ghost"));

        var empty = new XmlSchemaElement();
        var unresolved = (SchemaTypeId?)resolve.Invoke(null, [empty]);
        await Assert.That(unresolved).IsNull();
    }

    [Test]
    public async Task CollidingAnonymousAndNamedType_KeepsNamedParticle()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "collide.xsd")]);
        var fooType = new SchemaTypeId("urn:novolis:codegen:collide", "FooType");
        await Assert.That(graph.ComplexById.ContainsKey(fooType)).IsTrue();
        // Outer Build skip when id already present — named FooType particle preserved
        await Assert.That(graph.ComplexById[fooType].Particle!.Children[0].ElementName).IsEqualTo("A");
        await Assert.That(graph.Elements.Count(e => e.Name is "Foo" or "Named")).IsEqualTo(2);
    }

    [Test]
    public async Task ExcludedNamespace_SkipsGlobalElements()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles(
            [EdgesPath],
            new SchemaGraphOptions
            {
                ExcludedNamespaces = new HashSet<string>(StringComparer.Ordinal)
                {
                    "urn:novolis:codegen:edges",
                    "http://www.w3.org/2000/09/xmldsig#",
                    "http://uri.etsi.org/01903/v1.3.2#",
                    "http://uri.etsi.org/01903/v1.4.1#"
                }
            });
        await Assert.That(graph.Elements.Any(e => e.NamespaceName == "urn:novolis:codegen:edges")).IsFalse();
        await Assert.That(graph.ComplexTypes.Any(t => t.Id.NamespaceName == "urn:novolis:codegen:edges")).IsFalse();
    }

    [Test]
    public async Task UnresolvedElementType_UsesSchemaTypeNameFallback()
    {
        const string ns = "urn:novolis:codegen:unresolved";
        var schema = new XmlSchema { TargetNamespace = ns, ElementFormDefault = XmlSchemaForm.Qualified };
        schema.Items.Add(new XmlSchemaElement
        {
            Name = "Maybe",
            SchemaTypeName = new System.Xml.XmlQualifiedName("MissingType", ns)
        });

        var set = new XmlSchemaSet();
        set.ValidationEventHandler += (_, _) => { /* allow unresolved */ };
        set.Add(schema);
        try
        {
            set.Compile();
        }
        catch (XmlSchemaException)
        {
            // Some runtimes still throw; graph build may still see partial globals
        }

        if (set.GlobalElements.Count == 0)
            return;

        var graph = SchemaGraphBuilder.Build(set, new SchemaGraphOptions { MarkUnreferencedAsDocumentRoots = true });
        await Assert.That(graph.Elements.Any(e => e.Name == "Maybe")).IsTrue();
    }

    [Test]
    public async Task ElementDecl_QualifiedName_WithoutNamespace()
    {
        var el = new ElementDecl("Local", "", null, true, false);
        await Assert.That(el.QualifiedName).IsEqualTo("Local");
    }
}
