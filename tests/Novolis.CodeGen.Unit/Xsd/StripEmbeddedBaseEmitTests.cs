using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

namespace Novolis.CodeGen.Unit.Xsd;

public sealed class StripEmbeddedBaseEmitTests
{
    private static string FixturesDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Schemas");

    [Test]
    public async Task BaseEmitDeterministic_TwoEmitsMatch()
    {
        var a = HashEmit(EmitBilling());
        var b = HashEmit(EmitBilling());
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task BaseHasInterfaces_AndRecords()
    {
        var result = EmitBilling();
        var hasIface = result.Files.Any(f =>
            f.CompilationUnit.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Any());
        var hasRecord = result.Files.Any(f =>
            f.CompilationUnit.DescendantNodes().OfType<RecordDeclarationSyntax>().Any());
        await Assert.That(hasIface).IsTrue();
        await Assert.That(hasRecord).IsTrue();
    }

    [Test]
    public async Task NoByteArrays_InEmittedText()
    {
        var result = EmitBilling();
        foreach (var file in result.Files)
        {
            var text = SyntaxEmitWriter.Format(file.CompilationUnit);
            await Assert.That(text.Contains("byte[]")).IsFalse();
        }
    }

    [Test]
    public async Task BinaryObjectBecomesRef_OnAttachment()
    {
        var result = EmitBilling();
        var attachment = result.Files.Single(f => f.RelativePath.Contains("AttachmentBase", StringComparison.Ordinal));
        var text = SyntaxEmitWriter.Format(attachment.CompilationUnit);
        await Assert.That(text.Contains("BinaryObjectRef")).IsTrue();
        await Assert.That(text.Contains("EmbeddedDocumentBinaryObject")).IsTrue();

        var binRef = result.Files.Single(f => f.RelativePath == "BinaryObjectRef.g.cs");
        var binText = SyntaxEmitWriter.Format(binRef.CompilationUnit);
        await Assert.That(binText.Contains("MimeCode")).IsTrue();
        await Assert.That(binText.Contains("Filename")).IsTrue();
        await Assert.That(binText.Contains("Uri")).IsTrue();
    }

    [Test]
    public async Task BillingSpineOnDocs_ExtendsSharedInterface()
    {
        var result = EmitBilling();
        var spine = result.Files.Single(f => f.RelativePath == "IBillingDocumentBase.g.cs");
        await Assert.That(SyntaxEmitWriter.Format(spine.CompilationUnit).Contains("interface IBillingDocumentBase")).IsTrue();

        var invoice = result.Files.Single(f => f.RelativePath == "InvoiceBase.g.cs");
        var text = SyntaxEmitWriter.Format(invoice.CompilationUnit);
        await Assert.That(text.Contains("interface IInvoiceBase")).IsTrue();
        await Assert.That(text.Contains("IBillingDocumentBase")).IsTrue();
        await Assert.That(text.Contains("record InvoiceBase")).IsTrue();
    }

    [Test]
    public async Task InvoiceCreditNoteReminderScope_EmitsThreeRoots()
    {
        var result = EmitBilling();
        var names = result.Files.Select(f => f.RelativePath).ToHashSet(StringComparer.Ordinal);
        await Assert.That(names.Contains("InvoiceBase.g.cs")).IsTrue();
        await Assert.That(names.Contains("CreditNoteBase.g.cs")).IsTrue();
        await Assert.That(names.Contains("ReminderBase.g.cs")).IsTrue();
    }

    private static EmitResult EmitBilling()
    {
        var graph = SchemaGraphBuilder.BuildFromFiles([Path.Combine(FixturesDir, "billing.xsd")]);
        return new StripEmbeddedBaseProfile().Emit(graph, new EmitOptions
        {
            RootNamespace = "Billing.Base",
            BillingSpineInterfaceName = "IBillingDocumentBase",
            StripEmbeddedPolicy = StripEmbeddedPolicy.MetadataOnly,
            OneFilePerType = true
        });
    }

    private static string HashEmit(EmitResult result)
    {
        var sb = new StringBuilder();
        foreach (var file in result.Files.OrderBy(f => f.RelativePath, StringComparer.Ordinal))
        {
            sb.Append(file.RelativePath);
            sb.Append('\n');
            sb.Append(SyntaxEmitWriter.Format(file.CompilationUnit));
            sb.Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }
}
