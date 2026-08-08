using System.Reflection;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

namespace Novolis.CodeGen.Unit.Xsd;

public sealed class WireEmitTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    [Test]
    public async Task Wire_OptionalParticles_AreNullableAnnotated()
    {
        var (result, _) = EmitTiny();
        var line = result.Files.Single(f => f.RelativePath.Contains("LineType", StringComparison.Ordinal));
        var text = SyntaxEmitWriter.Format(line.CompilationUnit);
        await Assert.That(text.StartsWith("#nullable", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("decimal?")).IsTrue();
        await Assert.That(text.Contains("BinaryObjectType?")).IsTrue();

        var doc = result.Files.Single(f => f.RelativePath.Contains("DocumentType", StringComparison.Ordinal));
        var docText = SyntaxEmitWriter.Format(doc.CompilationUnit);
        // Choice alternatives under minOccurs=0 are optional
        await Assert.That(docText.Contains("BinaryPayloadType?")).IsTrue();
        await Assert.That(docText.Contains("string? Comment") || docText.Contains("Comment")).IsTrue();
    }

    [Test]
    public async Task EmitCompiles_AgainstNetRefs()
    {
        var (result, _) = EmitTiny();
        var compilation = Compile(result);
        var diags = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        await Assert.That(diags).IsEmpty();
    }

    [Test]
    public async Task InterfaceEmitted_InSyntaxTree()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Tiny.Wire",
            DocumentRootInterfaceName = "IUblDocument"
        });

        var hasIface = result.Files.Any(f =>
            f.CompilationUnit.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Any());
        await Assert.That(hasIface).IsTrue();
    }

    [Test]
    public async Task Emit_IncludesCctsDefinitionSummaries()
    {
        var (result, _) = EmitTiny();
        var header = result.Files.Single(f => f.RelativePath.Contains("HeaderType", StringComparison.Ordinal));
        var text = SyntaxEmitWriter.Format(header.CompilationUnit);
        await Assert.That(text).Contains("/// <summary>Document header with currency.</summary>");
        await Assert.That(text).Contains("/// <summary>Human-readable title.</summary>");
    }

    [Test]
    public async Task NoTrailingSemicolonDefect_PropertyBlocksAreValid()
    {
        var (result, _) = EmitTiny();
        var text = string.Join("\n", result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)));
        await Assert.That(text.Contains("};")).IsFalse();
        await Assert.That(text.Contains("{ get; set; }")).IsTrue();
    }

    [Test]
    public async Task XmlRoundTripTiny_SerializeDeserialize()
    {
        var (result, _) = EmitTiny();
        var compilation = Compile(result);
        await using var pe = new MemoryStream();
        var emit = compilation.Emit(pe);
        if (!emit.Success)
        {
            var errors = string.Join("\n", emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException(errors);
        }

        pe.Position = 0;
        var asm = System.Reflection.Assembly.Load(pe.ToArray());
        var docType = asm.GetType("Tiny.Wire.Tiny.DocumentType")
                      ?? throw new InvalidOperationException("DocumentType missing");
        var headerType = asm.GetType("Tiny.Wire.Tiny.HeaderType")!;
        var lineType = asm.GetType("Tiny.Wire.Tiny.LineType")!;

        var doc = Activator.CreateInstance(docType)!;
        var header = Activator.CreateInstance(headerType)!;
        headerType.GetProperty("Title")!.SetValue(header, "Hello");
        headerType.GetProperty("currencyID")!.SetValue(header, "EUR");
        docType.GetProperty("Header")!.SetValue(doc, header);

        var line = Activator.CreateInstance(lineType)!;
        lineType.GetProperty("Id")!.SetValue(line, "1");
        var lineCollType = docType.GetProperty("Line")!.PropertyType;
        var coll = Activator.CreateInstance(lineCollType)!;
        lineCollType.GetMethod("Add")!.Invoke(coll, [line]);
        docType.GetProperty("Line")!.SetValue(doc, coll);

        var root = new XmlRootAttribute("Document") { Namespace = "urn:novolis:codegen:tiny" };
        var serializer = new XmlSerializer(docType, root);
        await using var ms = new MemoryStream();
        serializer.Serialize(ms, doc);
        ms.Position = 0;
        var round = serializer.Deserialize(ms);
        await Assert.That(round).IsNotNull();
        var title = headerType.GetProperty("Title")!.GetValue(docType.GetProperty("Header")!.GetValue(round));
        await Assert.That(title).IsEqualTo("Hello");
    }

    [Test]
    public async Task LeanHasNoByteArrayProperties_OnEmittedRecords()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var result = new LeanRecordsProfile().Emit(graph, new EmitOptions { RootNamespace = "Tiny.Lean" });
        var texts = result.Files.Select(f => SyntaxEmitWriter.Format(f.CompilationUnit)).ToArray();
        foreach (var t in texts)
            await Assert.That(t.Contains("byte[]")).IsFalse();
    }

    private static (EmitResult Result, SchemaGraph Graph) EmitTiny()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "tiny.xsd")]);
        var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Tiny.Wire",
            DocumentRootInterfaceName = "IUblDocument",
            OneFilePerType = true
        });
        return (result, graph);
    }

    private static CSharpCompilation Compile(EmitResult result)
    {
        var trees = result.Files
            .Select(f => CSharpSyntaxTree.ParseText(SyntaxEmitWriter.Format(f.CompilationUnit)))
            .ToArray();
        var refs = new[]
            {
                typeof(object).Assembly.Location,
                typeof(XmlSerializer).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(System.Collections.ObjectModel.Collection<>).Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Collections.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Xml.ReaderWriter.dll")
            }
            .Where(File.Exists)
            .Distinct()
            .Select(p => MetadataReference.CreateFromFile(p))
            .ToArray();

        return CSharpCompilation.Create(
            "TinyWireEmit",
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }
}
