using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;
using Novolis.CodeGen.Pipeline;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class RoslynEmitWriterTests
{
    enum TestPhase { Emit }

    sealed class TestEmitContext : BindingEmitContext;

    sealed class AddUsingHook : ICodegenHook<TestPhase, TestEmitContext>
    {
        public int Order => 0;
        public TestPhase Phase => TestPhase.Emit;
        public CompilationUnitSyntax Transform(CompilationUnitSyntax unit, TestEmitContext context) =>
            SyntaxRewriters.EnsureUsing(unit, "System.Collections.Generic");
    }

    static readonly InteropPolicySpec EmptyPolicy = new([], [], null, false);

    static TestEmitContext CreateContext(MockFileSystem fs, string repoRoot, string relativeOutput)
    {
        var fragment = new InteropExportsFragment(
            "test", 1, null, null, "lib", EmptyPolicy, [], [new InteropImportSpec("Init", NativeSignature.Create(NativeType.Void))]);
        return new TestEmitContext
        {
            Environment = new CodegenEnvironment { FileSystem = fs, RepoRoot = repoRoot },
            OutputPath = fs.Path.Combine(repoRoot, relativeOutput),
            Fragment = fragment,
            ManifestSha256 = "abc123",
            RegenerateHint = "dotnet run -- codegen",
        };
    }

    [Test]
    public async Task WriteFile_applies_hooks_and_writes_to_virtual_fs()
    {
        var repoRoot = TestPaths.Root("codegen-emit");
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var context = CreateContext(fs, repoRoot, Path.Combine("generated", "Sample.g.cs"));
        const string raw = "namespace N { class C { void M() { var x = List<int>.Empty; } } }";

        RoslynEmitWriter<TestPhase, TestEmitContext>.WriteFile(
            raw,
            context,
            TestPhase.Emit,
            [new AddUsingHook()],
            FormatPolicy.NormalizeWhitespace);

        var outPath = TestPaths.Combine(repoRoot, "generated", "Sample.g.cs");
        await Assert.That(fs.FileExists(outPath)).IsTrue();
        var text = fs.File.ReadAllText(outPath);
        await Assert.That(text).Contains("System.Collections.Generic");
        await Assert.That(text.EndsWith('\n')).IsTrue();
    }

    [Test]
    public async Task WriteFile_roslyn_formatter_policy()
    {
        var repoRoot = TestPaths.Root("codegen-format");
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var context = CreateContext(fs, repoRoot, Path.Combine("generated", "Formatted.g.cs"));
        const string raw = "namespace N{class C{public void M(){}}";

        RoslynEmitWriter<TestPhase, TestEmitContext>.WriteFile(
            raw,
            context,
            TestPhase.Emit,
            [],
            FormatPolicy.RoslynFormatter);

        var text = fs.File.ReadAllText(TestPaths.Combine(repoRoot, "generated", "Formatted.g.cs"));
        await Assert.That(text).Contains("namespace N");
        await Assert.That(text).Contains("class C");
    }
}
