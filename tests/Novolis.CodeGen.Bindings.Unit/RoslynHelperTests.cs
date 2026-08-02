using Microsoft.CodeAnalysis.CSharp;
using Novolis.CodeGen.Bindings.Roslyn;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class RoslynHelperTests
{
    [Test]
    public async Task CodegenSyntaxParser_parses_compilation_unit()
    {
        const string source = "namespace N { public static class G { public static void M() {} } }";
        var unit = CodegenSyntaxParser.ParseGenerated(source);
        await Assert.That(unit.Members.Count).IsEqualTo(1);
        await Assert.That(unit.ToFullString()).Contains("namespace N");
    }

    [Test]
    public async Task SyntaxRewriters_ensure_using_adds_directive()
    {
        const string source = "namespace N { class C { void M() { var x = List<int>.Empty; } } }";
        var unit = CodegenSyntaxParser.ParseGenerated(source);
        var rewritten = SyntaxRewriters.EnsureUsing(unit, "System.Collections.Generic");
        var text = rewritten.ToFullString();
        await Assert.That(text).Contains("System.Collections.Generic");
    }

    [Test]
    public async Task SyntaxRewriters_ensure_using_is_idempotent_when_present()
    {
        const string source = "using System.Collections.Generic; namespace N { class C { } }";
        var unit = CodegenSyntaxParser.ParseGenerated(source);
        var before = unit.Usings.Count;
        var rewritten = SyntaxRewriters.EnsureUsing(unit, "System.Collections.Generic");
        await Assert.That(rewritten.Usings.Count).IsEqualTo(before);
        var again = SyntaxRewriters.EnsureUsing(rewritten, "System.Collections.Generic");
        await Assert.That(again.Usings.Count).IsEqualTo(before);
    }

    [Test]
    public async Task CompilationUnitComparer_detects_member_diff()
    {
        const string a = "class A { void M() {} void N() {} }";
        const string b = "class A { void M() {} }";
        await Assert.That(CompilationUnitComparer.AreStructurallyEquivalent(a, b)).IsFalse();
    }
}
