using Novolis.CodeGen.Bindings.Roslyn;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class CompilationUnitComparerTests
{
    [Test]
    public async Task AreStructurallyEquivalent_IgnoresInsignificantWhitespace()
    {
        const string a = "namespace N;\npublic class C { public int X { get; } }\n";
        const string b = "namespace N;\n\npublic class C\n{\n    public int X { get; }\n}\n";
        await Assert.That(CompilationUnitComparer.AreStructurallyEquivalent(a, b)).IsTrue();
    }

    [Test]
    public async Task AreStructurallyEquivalent_DifferentMembers_False()
    {
        const string a = "namespace N; public class A {}";
        const string b = "namespace N; public class B {}";
        await Assert.That(CompilationUnitComparer.AreStructurallyEquivalent(a, b)).IsFalse();
    }
}
