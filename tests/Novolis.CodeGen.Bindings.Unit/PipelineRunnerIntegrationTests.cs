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
            var beforeJson = File.ReadAllText(Path.Combine(stepDir, "result.json"));
            File.WriteAllText(Path.Combine(stepDir, "step.log"), "# previous-success\n");
            var beforeLog = File.ReadAllText(Path.Combine(stepDir, "step.log"));

            var exit = await runner.RunProfileAsync(["emit"], force: false);
            await Assert.That(exit).IsEqualTo(0);
            var doc = StepResultWriter.TryRead(stepDir);
            await Assert.That(doc!.Status).IsEqualTo(StepStatus.Succeeded);
            await Assert.That(File.ReadAllText(Path.Combine(stepDir, "result.json"))).IsEqualTo(beforeJson);
            await Assert.That(File.ReadAllText(Path.Combine(stepDir, "step.log"))).IsEqualTo(beforeLog);
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
