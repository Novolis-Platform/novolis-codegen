using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;
using Novolis.CodeGen.Pipeline;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class HookDiscoveryTests
{
    public enum TestPhase { Emit }

    public sealed class TestEmitContext : BindingEmitContext;

    public sealed class DiscoveredHook : ICodegenHook<TestPhase, TestEmitContext>
    {
        public int Order => 5;
        public TestPhase Phase => TestPhase.Emit;
        public CompilationUnitSyntax Transform(CompilationUnitSyntax unit, TestEmitContext context) => unit;
    }

    [Test]
    public async Task Discover_finds_and_orders_hooks()
    {
        var hooks = HookDiscovery.Discover<TestPhase, TestEmitContext>(typeof(HookDiscoveryTests).Assembly);
        await Assert.That(hooks.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(hooks[0]).IsTypeOf<DiscoveredHook>();
    }

    [Test]
    public async Task Discover_deduplicates_assemblies()
    {
        var asm = typeof(HookDiscoveryTests).Assembly;
        var hooks = HookDiscovery.Discover<TestPhase, TestEmitContext>(asm, asm);
        await Assert.That(hooks.Count).IsEqualTo(
            HookDiscovery.Discover<TestPhase, TestEmitContext>(asm).Count);
    }
}
