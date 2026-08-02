using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;
using Novolis.CodeGen.Pipeline;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class PipelineRunnerIntegrationTests
{
    [Test]
    public async Task RunStepAsync_executes_and_writes_result()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-run-");
        try
        {
            var layout = new TestPipelineLayout(temp.FullName);
            var runner = new PipelineRunner([new SuccessStep("emit")], layout);
            var exit = await runner.RunStepAsync("emit", force: true);
            await Assert.That(exit).IsEqualTo(0);

            var doc = StepResultWriter.TryRead(layout.StepDir("emit"));
            await Assert.That(doc).IsNotNull();
            await Assert.That(doc!.Status).IsEqualTo(StepStatus.Succeeded);
            await Assert.That(File.Exists(Path.Combine(layout.StepDir("emit"), "step.log"))).IsTrue();
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task RunProfileAsync_skips_when_outputs_current()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-skiprun-");
        try
        {
            var input = Path.Combine(temp.FullName, "src", "in.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(input)!);
            await File.WriteAllTextAsync(input, "same");
            var output = Path.Combine(temp.FullName, "out", "gen.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllTextAsync(output, "// ok");

            var layout = new TestPipelineLayout(temp.FullName);
            var step = new SuccessStep("emit", ["src/in.txt"], ["out/gen.cs"]);
            var stepDir = layout.StepDir("emit");
            Directory.CreateDirectory(stepDir);
            StepResultWriter.Write(stepDir, new StepResultDocument
            {
                StepId = "emit",
                Status = StepStatus.Succeeded,
                Inputs = StepFileFingerprint.HashFiles(["src/in.txt"], temp.FullName),
                Outputs = [new StepOutputRecord { Path = "out/gen.cs", Sha256 = StepFileFingerprint.Sha256Hex(output) }],
            });

            var runner = new PipelineRunner([step], layout);
            var exit = await runner.RunProfileAsync(["emit"], force: false);
            await Assert.That(exit).IsEqualTo(0);
            var doc = StepResultWriter.TryRead(stepDir);
            await Assert.That(doc!.Status).IsEqualTo(StepStatus.Skipped);
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task RunStepAsync_unknown_id_throws()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-unknown-");
        try
        {
            var runner = new PipelineRunner([], new TestPipelineLayout(temp.FullName));
            await Assert.That(() => runner.RunStepAsync("missing", force: true)).Throws<InvalidOperationException>();
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task RunStepAsync_catches_step_exception()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-ex-");
        try
        {
            var layout = new TestPipelineLayout(temp.FullName);
            var runner = new PipelineRunner([new ThrowingStep("boom")], layout);
            var exit = await runner.RunStepAsync("boom", force: true);
            await Assert.That(exit).IsEqualTo(1);
            var doc = StepResultWriter.TryRead(layout.StepDir("boom"));
            await Assert.That(doc!.Status).IsEqualTo(StepStatus.Failed);
            await Assert.That(doc.Error!.Message).Contains("pipeline boom");
        }
        finally
        {
            temp.Delete(true);
        }
    }

    sealed class TestPipelineLayout(string repoRoot) : IPipelineLayout
    {
        public string RepoRoot => repoRoot;
        public string StepsRoot => Path.Combine(repoRoot, "steps");
        public string ManifestDir => Path.Combine(repoRoot, "manifests");
        public string StepDir(string stepId) => Path.Combine(StepsRoot, stepId);
        public string StepArtifactsDir(string stepId) => Path.Combine(StepDir(stepId), "artifacts");
    }

    sealed class SuccessStep(string id, string[]? inputs = null, string[]? outputs = null) : IPipelineStep
    {
        public string Id => id;
        public string Description => "success";
        public IReadOnlyList<string> DependsOn => [];
        public IReadOnlyList<string> InputPaths(PipelineContext context) => inputs ?? [];
        public IReadOnlyList<string> ExpectedOutputPaths(PipelineContext context) => outputs ?? [];
        public ValueTask<StepExecutionResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new StepExecutionResult { Status = StepStatus.Succeeded });
    }

    sealed class ThrowingStep(string id) : IPipelineStep
    {
        public string Id => id;
        public string Description => "throws";
        public IReadOnlyList<string> DependsOn => [];
        public IReadOnlyList<string> InputPaths(PipelineContext context) => [];
        public IReadOnlyList<string> ExpectedOutputPaths(PipelineContext context) => [];
        public ValueTask<StepExecutionResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("pipeline boom");
    }
}

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
            "test", 1, null, null, "lib", EmptyPolicy, [], [new InteropImportSpec("Init", InteropTemplate.VoidVoid)]);
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
        const string repoRoot = @"C:\codegen-emit";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var context = CreateContext(fs, repoRoot, @"generated\Sample.g.cs");
        const string raw = "namespace N { class C { void M() { var x = List<int>.Empty; } } }";

        RoslynEmitWriter<TestPhase, TestEmitContext>.WriteFile(
            raw,
            context,
            TestPhase.Emit,
            [new AddUsingHook()],
            FormatPolicy.NormalizeWhitespace);

        await Assert.That(fs.FileExists(@"C:\codegen-emit\generated\Sample.g.cs")).IsTrue();
        var text = fs.File.ReadAllText(@"C:\codegen-emit\generated\Sample.g.cs");
        await Assert.That(text).Contains("System.Collections.Generic");
        await Assert.That(text.EndsWith('\n')).IsTrue();
    }

    [Test]
    public async Task WriteFile_roslyn_formatter_policy()
    {
        const string repoRoot = @"C:\codegen-format";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var context = CreateContext(fs, repoRoot, @"generated\Formatted.g.cs");
        const string raw = "namespace N{class C{public void M(){}}";

        RoslynEmitWriter<TestPhase, TestEmitContext>.WriteFile(
            raw,
            context,
            TestPhase.Emit,
            [],
            FormatPolicy.RoslynFormatter);

        var text = fs.File.ReadAllText(@"C:\codegen-format\generated\Formatted.g.cs");
        await Assert.That(text).Contains("namespace N");
        await Assert.That(text).Contains("class C");
    }
}

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
