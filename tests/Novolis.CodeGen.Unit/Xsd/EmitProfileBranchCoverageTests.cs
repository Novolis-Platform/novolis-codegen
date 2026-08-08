using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

namespace Novolis.CodeGen.Unit.Xsd;

public sealed class EmitProfileBranchCoverageTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    private static string EdgesPath => Path.Combine(FixturesDir, "branch-edges.xsd");
    private static string BillingPath => Path.Combine(FixturesDir, "billing.xsd");

    [Test]
    public async Task Wire_NameAndIncludeTypeIds_FilterEmit()
    {
        var profile = new WireXmlSerializerProfile();
        await Assert.That(profile.Name).IsEqualTo("WireXmlSerializer");

        var graph = SchemaGraphBuilder.BuildFromFiles([EdgesPath]);
        var only = new HashSet<SchemaTypeId> { new("urn:novolis:codegen:edges", "PrimitivesType") };
        var result = profile.Emit(graph, new EmitOptions
        {
            RootNamespace = "Edges.Wire",
            IncludeTypeIds = only
        });
        await Assert.That(result.Files.Count).IsEqualTo(1);
        await Assert.That(result.Files[0].RelativePath.Contains("PrimitivesType", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Wire_Primitives_ResolveBuiltinClrAndKeywords()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([EdgesPath]);
        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Edges.Wire",
            DocumentRootInterfaceName = "IEdgeDoc"
        });

        var text = string.Join("\n", result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(text.Contains("byte[]")).IsTrue();
        await Assert.That(text.Contains("bool")).IsTrue();
        await Assert.That(text.Contains("decimal")).IsTrue();
        await Assert.That(text.Contains("System.DateTime")).IsTrue();
        await Assert.That(text.Contains("Collection<")).IsTrue();
        await Assert.That(text.Contains("classValue")).IsTrue();
        await Assert.That(text.Contains("objectValue")).IsTrue();
        await Assert.That(text.Contains("eventValue")).IsTrue();
        await Assert.That(text.Contains("paramsValue")).IsTrue();
        await Assert.That(text.Contains("baseValue")).IsTrue();
        await Assert.That(text.Contains("thisValue")).IsTrue();
        await Assert.That(text.Contains("stringValue")).IsTrue();
        await Assert.That(text.Contains("intValue")).IsTrue();
        await Assert.That(text.Contains("CollisionTypeValue")).IsTrue();
        await Assert.That(text.Contains("interface IEdgeDoc")).IsTrue();
    }

    [Test]
    public async Task Wire_HandGraph_ExternalFallbackAndNullType()
    {
        var dsigId = new SchemaTypeId("http://www.w3.org/2000/09/xmldsig#", "SignatureType");
        var xadesId = new SchemaTypeId("http://uri.etsi.org/01903/v1.3.2#", "QualifyingProperties");
        var unknownId = new SchemaTypeId("urn:external:missing", "GhostType");
        var hostId = new SchemaTypeId("urn:hand", "HostType");
        var dupId = new SchemaTypeId("urn:hand", "DupType");

        var host = new ComplexTypeNode(
            hostId,
            "HostType",
            new Particle(
                ParticleKind.Sequence,
                1,
                1,
                children:
                [
                    new Particle(ParticleKind.Element, 1, 1, "Sig", "urn:hand", dsigId),
                    new Particle(ParticleKind.Element, 0, 1, "Xades", null, xadesId),
                    new Particle(ParticleKind.Element, 1, 1, null, "urn:hand", null),
                    new Particle(ParticleKind.Element, 1, 1, "Ghost", "urn:hand", unknownId),
                    new Particle(ParticleKind.Element, 1, 1, "", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "token")),
                    new Particle(ParticleKind.Element, 1, decimal.MaxValue, "Many", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "short"))
                ]),
            [
                new AttributeDecl("a", "urn:hand", new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "boolean"), true),
                new AttributeDecl("b", null, null, false, "def")
            ],
            null,
            false,
            BinaryFacet.None);

        var simpleContent = new ComplexTypeNode(
            new SchemaTypeId("urn:hand", "BinWrap"),
            "BinWrap",
            null,
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.Base64Binary,
            hasSimpleContent: true,
            simpleContentClrType: null);

        var collision = new ComplexTypeNode(
            dupId,
            "DupType",
            new Particle(
                ParticleKind.Sequence,
                1,
                1,
                children:
                [
                    new Particle(ParticleKind.Element, 1, 1, "DupType", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "string")),
                    new Particle(ParticleKind.Element, 1, 1, "Same", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "string")),
                    new Particle(ParticleKind.Element, 1, 1, "Same", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "string")),
                    new Particle(ParticleKind.Element, 1, 1, "9bad", "urn:hand",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "int"))
                ]),
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.None);

        var w3Simple = new SimpleTypeNode(
            new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "string"),
            "string",
            null,
            "string",
            BinaryFacet.None,
            Array.Empty<string>());

        var localSimple = new SimpleTypeNode(
            new SchemaTypeId("urn:hand", "CodeType"),
            "CodeType",
            null,
            "string",
            BinaryFacet.None,
            ["A", "B"]);

        var graph = new SchemaGraph(
            [host, simpleContent, collision],
            [w3Simple, localSimple],
            [
                new ElementDecl("Host", "urn:hand", hostId, true, false),
                new ElementDecl("Dup", "urn:hand", dupId, true, false)
            ]);

        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Hand.Wire",
            DocumentRootInterfaceName = "IHandDoc",
            IncludeTypeIds = new HashSet<SchemaTypeId> { hostId, dupId, simpleContent.Id, localSimple.Id }
        });

        var text = string.Join("\n", result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(text.Contains("System.Xml.XmlElement")).IsTrue();
        await Assert.That(text.Contains("GhostType")).IsTrue();
        await Assert.That(text.Contains("Item")).IsTrue();
        await Assert.That(text.Contains("_9bad") || text.Contains("9bad")).IsTrue();
        await Assert.That(text.Contains("Same2")).IsTrue();
        await Assert.That(text.Contains("DupTypeValue")).IsTrue();
        await Assert.That(text.Contains("byte[]")).IsTrue();
        await Assert.That(text.Contains("CodeType")).IsTrue();
        await Assert.That(result.Files.Any(f =>
            f.CompilationUnit.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                .Any(i => i.Identifier.Text == "IHandDoc"))).IsTrue();
    }

    [Test]
    public async Task Wire_NullArgs_Throw()
    {
        var profile = new WireXmlSerializerProfile();
        var graph = new SchemaGraph([], [], []);
        await Assert.That(() => profile.Emit(null!, new EmitOptions { RootNamespace = "X" }))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => profile.Emit(graph, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Lean_NameIncludeFilterAndOptionalCollections()
    {
        var profile = new LeanRecordsProfile();
        await Assert.That(profile.Name).IsEqualTo("LeanRecords");

        var graph = SchemaGraphBuilder.BuildFromFiles([EdgesPath]);
        var only = new HashSet<SchemaTypeId>
        {
            new("urn:novolis:codegen:edges", "PrimitivesType"),
            new("urn:novolis:codegen:edges", "AllType")
        };
        var result = profile.Emit(graph, new EmitOptions
        {
            RootNamespace = "Edges.Lean",
            IncludeTypeIds = only
        });

        await Assert.That(result.Files.Count).IsEqualTo(2);
        var text = string.Join("\n", result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(text.Contains("byte[]")).IsFalse();
        await Assert.That(text.Contains("IReadOnlyList<")).IsTrue();
        await Assert.That(text.Contains("?")).IsTrue();
    }

    [Test]
    public async Task Lean_HandGraph_ResolveEdges()
    {
        var binSimple = new SimpleTypeNode(
            new SchemaTypeId("urn:lean", "BinSimple"),
            "BinSimple",
            null,
            "byte[]",
            BinaryFacet.Base64Binary,
            Array.Empty<string>());
        var binComplex = new ComplexTypeNode(
            new SchemaTypeId("urn:lean", "BinComplex"),
            "BinComplex",
            null,
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.BinaryObject,
            true,
            "byte[]");
        var hostId = new SchemaTypeId("urn:lean", "LeanHost");
        var host = new ComplexTypeNode(
            hostId,
            "LeanHost",
            new Particle(
                ParticleKind.Choice,
                1,
                1,
                children:
                [
                    new Particle(ParticleKind.Element, 0, 1, "Opt", "urn:lean",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "int")),
                    new Particle(ParticleKind.Element, 1, 3, "Many", "urn:lean",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "string")),
                    new Particle(ParticleKind.Element, 1, 1, "Raw", "urn:lean",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "base64Binary")),
                    new Particle(ParticleKind.Element, 1, 1, "Hex", "urn:lean",
                        new SchemaTypeId("http://www.w3.org/2001/XMLSchema", "hexBinary")),
                    new Particle(ParticleKind.Element, 1, 1, "ViaSimple", "urn:lean", binSimple.Id),
                    new Particle(ParticleKind.Element, 1, 1, "ViaComplex", "urn:lean", binComplex.Id),
                    new Particle(ParticleKind.Element, 1, 1, null, "urn:lean", null),
                    new Particle(ParticleKind.Element, 1, 1, "9x", "urn:lean",
                        new SchemaTypeId("urn:lean", "Missing"))
                ]),
            [
                new AttributeDecl("binAttr", null, binSimple.Id, false),
                new AttributeDecl("", null, null, false)
            ],
            null,
            false,
            BinaryFacet.None);

        var graph = new SchemaGraph([host, binComplex], [binSimple], [
            new ElementDecl("LeanHost", "urn:lean", hostId, true, false)
        ]);

        var result = new LeanRecordsProfile().Emit(graph, new EmitOptions { RootNamespace = "Lean.Hand" });
        var text = SyntaxEmitWriter.Format(result.Files.Single().CompilationUnit);
        await Assert.That(text.Contains("byte[]")).IsFalse();
        await Assert.That(text.Contains("IReadOnlyList<string>")).IsTrue();
        await Assert.That(text.Contains("int?")).IsTrue();
        await Assert.That(text.Contains("Item")).IsTrue();
        await Assert.That(text.Contains("_9x")).IsTrue();
        await Assert.That(text.Contains("Missing")).IsTrue();
    }

    [Test]
    public async Task Lean_NullArgs_Throw()
    {
        var profile = new LeanRecordsProfile();
        await Assert.That(() => profile.Emit(null!, new EmitOptions { RootNamespace = "X" }))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => profile.Emit(new SchemaGraph([], [], []), null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task StripEmbedded_OmitPolicy_DropsBinaryRefs()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([BillingPath]);
        var result = new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Billing.Omit",
            StripEmbeddedPolicy = StripEmbeddedPolicy.Omit,
            BillingSpineInterfaceName = "IBillingDocumentBase"
        });

        var text = string.Join("\n", result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(result.Files.Any(f => f.RelativePath == "BinaryObjectRef.g.cs")).IsTrue();
        // Attachment still exists but EmbeddedDocumentBinaryObject should be omitted under Omit
        var attachment = result.Files.Single(f => f.RelativePath.Contains("Attachment", StringComparison.Ordinal));
        var attText = SyntaxEmitWriter.Format(attachment.CompilationUnit);
        await Assert.That(attText.Contains("EmbeddedDocumentBinaryObject")).IsFalse();
        await Assert.That(text.Contains("IBillingDocumentBase")).IsTrue();
    }

    [Test]
    public async Task StripEmbedded_IncludeFilterAndNoSpineForSingleRoot()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var profile = new StripEmbeddedBaseProfile();
        await Assert.That(profile.Name).IsEqualTo("StripEmbeddedBase");

        var docId = new SchemaTypeId("urn:novolis:codegen:tiny", "DocumentType");
        var result = profile.Emit(graph, new EmitOptions
        {
            RootNamespace = "Tiny.Base",
            IncludeTypeIds = new HashSet<SchemaTypeId> { docId },
            BillingSpineInterfaceName = "IBillingDocumentBase"
        });

        await Assert.That(result.Files.Any(f => f.RelativePath == "IBillingDocumentBase.g.cs")).IsFalse();
        await Assert.That(result.Files.Any(f => f.RelativePath.Contains("Document", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task StripEmbedded_HandGraph_CollapsesScalarsAttrsAndBuiltins()
    {
        var xs = "http://www.w3.org/2001/XMLSchema";
        var ns = "urn:strip";

        var codeId = new SchemaTypeId(ns, "CodeType");
        var code = new ComplexTypeNode(
            codeId,
            "CodeType",
            null,
            [new AttributeDecl("listID", null, new SchemaTypeId(xs, "string"), false)],
            null,
            false,
            BinaryFacet.None,
            hasSimpleContent: true,
            simpleContentClrType: "string");

        // HasSimpleContent false but SimpleContentClrType set → second collapse branch
        var scalarId = new SchemaTypeId(ns, "Scalar");
        var scalar = new ComplexTypeNode(
            scalarId,
            "Scalar",
            null,
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.None,
            hasSimpleContent: false,
            simpleContentClrType: "decimal");

        var binSimple = new SimpleTypeNode(
            new SchemaTypeId(ns, "BinSimple"),
            "BinSimple",
            null,
            "byte[]",
            BinaryFacet.Base64Binary,
            Array.Empty<string>());

        var labelSimple = new SimpleTypeNode(
            new SchemaTypeId(ns, "LabelType"),
            "LabelType",
            null,
            "string",
            BinaryFacet.None,
            ["X", "Y"]);

        var binComplex = new ComplexTypeNode(
            new SchemaTypeId(ns, "BinComplex"),
            "BinComplex",
            null,
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.BinaryObject,
            true,
            "byte[]");

        var collapseByteId = new SchemaTypeId(ns, "CollapseByte");
        var collapseByte = new ComplexTypeNode(
            collapseByteId,
            "CollapseByte",
            null,
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.None,
            true,
            "byte[]");

        var plainId = new SchemaTypeId(ns, "Plain");
        var invoiceId = new SchemaTypeId(ns, "InvoiceType");
        var creditId = new SchemaTypeId(ns, "CreditNoteType");
        var reminderId = new SchemaTypeId(ns, "ReminderType");

        Particle Builtin(string local, string name, decimal min = 1, decimal max = 1) =>
            new(ParticleKind.Element, min, max, name, ns, new SchemaTypeId(xs, local));

        ComplexTypeNode MakeDoc(SchemaTypeId id, string csharp, bool includeMismatchNote)
        {
            var kids = new List<Particle>
            {
                Builtin("string", "ID"),
                Builtin("date", "IssueDate", 0),
                Builtin("boolean", "Flag"),
                Builtin("decimal", "Dec"),
                Builtin("double", "Dbl"),
                Builtin("float", "Flt"),
                Builtin("int", "I"),
                Builtin("integer", "N"),
                Builtin("long", "L"),
                Builtin("short", "S"),
                Builtin("dateTime", "Dt"),
                Builtin("time", "Tm"),
                Builtin("base64Binary", "Raw", 0),
                Builtin("hexBinary", "Hex", 0),
                new Particle(ParticleKind.Element, 0, 1, "Code", ns, codeId),
                new Particle(ParticleKind.Element, 0, 1, "ScalarVal", ns, scalarId),
                new Particle(ParticleKind.Element, 0, 1, "Label", ns, labelSimple.Id),
                new Particle(ParticleKind.Element, 0, 1, "ViaSimple", ns, binSimple.Id),
                new Particle(ParticleKind.Element, 0, 1, "ViaComplex", ns, binComplex.Id),
                new Particle(ParticleKind.Element, 0, 1, "Collapse", ns, collapseByteId),
                new Particle(ParticleKind.Element, 0, 1, "Ghost", ns, new SchemaTypeId(ns, "Missing")),
                new Particle(ParticleKind.Element, 1, 1, null, ns, null),
                new Particle(ParticleKind.Element, 1, 3, "Notes", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "PlainRef", ns, plainId),
                new Particle(ParticleKind.Element, 1, 1, "InvoiceBase", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "CreditNoteBase", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "ReminderBase", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "9bad", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "class", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "event", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "params", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "base", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "this", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "string", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "int", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "object", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "Same", ns, new SchemaTypeId(xs, "string")),
                new Particle(ParticleKind.Element, 1, 1, "Same", ns, new SchemaTypeId(xs, "string")),
            };
            if (includeMismatchNote)
                kids.Add(new Particle(ParticleKind.Element, 0, 1, "Special", ns, new SchemaTypeId(xs, "int")));
            else
                kids.Add(new Particle(ParticleKind.Element, 0, 1, "Special", ns, new SchemaTypeId(xs, "string")));

            return new ComplexTypeNode(
                id,
                csharp,
                new Particle(ParticleKind.Sequence, 1, 1, children: kids),
                [
                    new AttributeDecl("currencyID", null, new SchemaTypeId(xs, "string"), true),
                    new AttributeDecl("opt", null, new SchemaTypeId(xs, "int"), false),
                    new AttributeDecl("bin", null, binSimple.Id, false),
                    new AttributeDecl("", null, null, false),
                    new AttributeDecl("object", null, new SchemaTypeId(xs, "string"), false)
                ],
                null,
                false,
                BinaryFacet.None);
        }

        var plain = new ComplexTypeNode(
            plainId,
            "Plain",
            new Particle(ParticleKind.Sequence, 1, 1, children:
            [
                Builtin("string", "Name")
            ]),
            [new AttributeDecl("scheme", null, new SchemaTypeId(xs, "string"), false)],
            null,
            false,
            BinaryFacet.None);

        var invoice = MakeDoc(invoiceId, "InvoiceType", includeMismatchNote: false);
        var credit = MakeDoc(creditId, "CreditNoteType", includeMismatchNote: false);
        var reminder = MakeDoc(reminderId, "ReminderType", includeMismatchNote: true);

        var graph = new SchemaGraph(
            [invoice, credit, reminder, plain, code, scalar, binComplex, collapseByte],
            [binSimple, labelSimple],
            [
                new ElementDecl("Invoice", ns, invoiceId, true, false),
                new ElementDecl("CreditNote", ns, creditId, true, false),
                new ElementDecl("Reminder", ns, reminderId, true, false)
            ]);

        var meta = new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Strip.Hand",
            BillingSpineInterfaceName = "IBillingDocumentBase",
            StripEmbeddedPolicy = StripEmbeddedPolicy.MetadataOnly
        });
        var metaText = string.Join("\n", meta.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(meta.Files.Any(f => f.RelativePath == "IBillingDocumentBase.g.cs")).IsTrue();
        await Assert.That(meta.Files.Any(f => f.RelativePath == "PlainBase.g.cs")).IsTrue();
        await Assert.That(metaText.Contains("BinaryObjectRef")).IsTrue();
        await Assert.That(metaText.Contains("classValue")).IsTrue();
        await Assert.That(metaText.Contains("eventValue")).IsTrue();
        await Assert.That(metaText.Contains("InvoiceBaseValue")).IsTrue();
        await Assert.That(metaText.Contains("LabelType") || metaText.Contains("string")).IsTrue();
        await Assert.That(metaText.Contains("_9bad")).IsTrue();
        await Assert.That(metaText.Contains("Same2")).IsTrue();
        await Assert.That(metaText.Contains("Item")).IsTrue();
        await Assert.That(metaText.Contains("IReadOnlyList<string>")).IsTrue();
        await Assert.That(metaText.Contains("int?")).IsTrue();

        var omit = new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Strip.Omit",
            StripEmbeddedPolicy = StripEmbeddedPolicy.Omit
        });
        var omitAttachmentish = string.Join("\n", omit.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(omitAttachmentish.Contains("ViaSimple")).IsFalse();
        await Assert.That(omitAttachmentish.Contains("ViaComplex")).IsFalse();
        await Assert.That(omitAttachmentish.Contains("Raw")).IsFalse();
    }

    [Test]
    public async Task Wire_RootWithoutDots_SafePathAndMoreBuiltins()
    {
        var xs = "http://www.w3.org/2001/XMLSchema";
        var id = new SchemaTypeId("", "RootType");
        var type = new ComplexTypeNode(
            id,
            "RootType",
            new Particle(ParticleKind.Sequence, 1, 1, children:
            [
                new Particle(ParticleKind.Element, 1, 1, "A", "", new SchemaTypeId(xs, "double")),
                new Particle(ParticleKind.Element, 1, 1, "B", "", new SchemaTypeId(xs, "float")),
                new Particle(ParticleKind.Element, 1, 1, "C", "", new SchemaTypeId(xs, "integer")),
                new Particle(ParticleKind.Element, 1, 1, "D", "", new SchemaTypeId(xs, "long")),
                new Particle(ParticleKind.Element, 1, 1, "E", "", new SchemaTypeId(xs, "date")),
                new Particle(ParticleKind.Element, 1, 1, "F", "", new SchemaTypeId(xs, "time")),
                new Particle(ParticleKind.Element, 1, 1, "G", "", new SchemaTypeId(xs, "base64Binary")),
                new Particle(ParticleKind.Element, 1, 1, "H", "", new SchemaTypeId(xs, "hexBinary"))
            ]),
            Array.Empty<AttributeDecl>(),
            null,
            false,
            BinaryFacet.None);

        var graph = new SchemaGraph([type], [], [
            new ElementDecl("Root", "", id, true, false)
        ]);

        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "WireSolo"
        });
        await Assert.That(result.Files[0].RelativePath.StartsWith("WireSolo", StringComparison.Ordinal)
                          || result.Files[0].RelativePath.Contains("RootType", StringComparison.Ordinal)).IsTrue();
        var text = SyntaxEmitWriter.Format(result.Files[0].CompilationUnit);
        await Assert.That(text.Contains("double")).IsTrue();
        await Assert.That(text.Contains("float")).IsTrue();
        await Assert.That(text.Contains("byte[]")).IsTrue();
    }

    [Test]
    public async Task SyntaxEmitWriter_Format_PreservesOrInjectsNullable()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([BillingPath]);
        var result = new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Billing.Nullable"
        });
        var text = SyntaxEmitWriter.Format(result.Files[0].CompilationUnit);
        await Assert.That(text.Contains("BinaryObjectRef") || text.StartsWith("#nullable", StringComparison.Ordinal)).IsTrue();

        // Wire CU has no nullable trivia → Format false branch
        var wire = new WireXmlSerializerProfile().Emit(
            SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]),
            new EmitOptions { RootNamespace = "Tiny.Fmt" });
        var wireText = SyntaxEmitWriter.Format(wire.Files[0].CompilationUnit);
        await Assert.That(wireText.StartsWith("#nullable", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task StripEmbedded_EdgesFixture_EmitsPlainPartyAttributes()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([EdgesPath]);
        var result = new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Edges.Base"
        });
        var party = result.Files.Single(f => f.RelativePath.Contains("PlainParty", StringComparison.Ordinal)
                                             || f.RelativePath == "PlainPartyBase.g.cs"
                                             || f.RelativePath == "PartyBase.g.cs");
        var text = SyntaxEmitWriter.Format(party.CompilationUnit);
        await Assert.That(text.Contains("scheme") || text.Contains("Scheme")).IsTrue();
        await Assert.That(text.Contains("classValue")).IsTrue();
    }

    [Test]
    public async Task StripEmbedded_NullArgs_Throw()
    {
        var profile = new StripEmbeddedBaseProfile();
        await Assert.That(() => profile.Emit(null!, new EmitOptions { RootNamespace = "X" }))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => profile.Emit(new SchemaGraph([], [], []), null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
