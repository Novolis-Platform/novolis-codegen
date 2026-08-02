using Novolis.CodeGen.Pipeline;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class PipelineStepTests
{
    [Test]
    public async Task StepFileFingerprint_hashes_files_and_bytes()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-fp-");
        try
        {
            var input = Path.Combine(temp.FullName, "input.txt");
            await File.WriteAllTextAsync(input, "hello");
            var hex = StepFileFingerprint.Sha256Hex(input);
            await Assert.That(hex).IsEqualTo(StepFileFingerprint.Sha256Hex("hello"u8.ToArray()));

            var map = StepFileFingerprint.HashFiles(["input.txt"], temp.FullName);
            await Assert.That(map.Count).IsEqualTo(1);
            await Assert.That(map["input.txt"]).IsEqualTo(hex);

            var outputs = StepFileFingerprint.DescribeOutputs(["input.txt"], temp.FullName);
            await Assert.That(outputs.Count).IsEqualTo(1);
            await Assert.That(outputs[0].Bytes).IsEqualTo(new FileInfo(input).Length);
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task StepResultWriter_round_trips_result_json()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-result-");
        try
        {
            var doc = new StepResultDocument
            {
                StepId = "emit",
                Status = StepStatus.Succeeded,
                Inputs = new Dictionary<string, string>(StringComparer.Ordinal) { ["a.txt"] = "abc" },
                Outputs = [new StepOutputRecord { Path = "out.cs", Sha256 = "dead", Bytes = 10 }],
            };
            StepResultWriter.Write(temp.FullName, doc);
            var read = StepResultWriter.TryRead(temp.FullName);
            await Assert.That(read).IsNotNull();
            await Assert.That(read!.StepId).IsEqualTo("emit");
            await Assert.That(read.Status).IsEqualTo(StepStatus.Succeeded);
            await Assert.That(read.Inputs["a.txt"]).IsEqualTo("abc");
            await Assert.That(read.Outputs[0].Sha256).IsEqualTo("dead");
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task StepSkipEvaluator_skips_when_inputs_and_outputs_unchanged()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-skip-");
        try
        {
            var inputPath = Path.Combine(temp.FullName, "src", "manifest.json");
            Directory.CreateDirectory(Path.GetDirectoryName(inputPath)!);
            await File.WriteAllTextAsync(inputPath, "{}");
            var outputPath = Path.Combine(temp.FullName, "generated", "out.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, "// generated");
            var outputHash = StepFileFingerprint.Sha256Hex(outputPath);

            var layout = new TestLayout(temp.FullName);
            var context = new PipelineContext
            {
                Layout = layout,
                Log = TextWriter.Null,
                Force = false,
            };
            var step = new TestStep("emit", ["src/manifest.json"], ["generated/out.cs"]);
            var previous = new StepResultDocument
            {
                StepId = "emit",
                Status = StepStatus.Succeeded,
                Inputs = StepFileFingerprint.HashFiles(["src/manifest.json"], temp.FullName),
                Outputs = [new StepOutputRecord { Path = "generated/out.cs", Sha256 = outputHash }],
            };

            var skip = StepSkipEvaluator.ShouldSkip(step, context, previous, out var reason);
            await Assert.That(skip).IsTrue();
            await Assert.That(reason).IsEqualTo("outputs up to date");
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task PipelineContext_exposes_layout_shortcuts()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-ctx-");
        try
        {
            var layout = new TestLayout(temp.FullName);
            var context = new PipelineContext { Layout = layout, Log = TextWriter.Null, Force = false };
            await Assert.That(context.RepoRoot).IsEqualTo(temp.FullName);
            await Assert.That(context.StepsRoot).IsEqualTo(Path.Combine(temp.FullName, "steps"));
            await Assert.That(context.StepDir("emit")).IsEqualTo(Path.Combine(temp.FullName, "steps", "emit"));
            await Assert.That(context.StepArtifactsDir("emit")).IsEqualTo(Path.Combine(temp.FullName, "steps", "emit", "artifacts"));
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task StepSkipEvaluator_does_not_skip_when_output_hash_mismatch()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-hash-");
        try
        {
            var inputPath = Path.Combine(temp.FullName, "src", "in.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(inputPath)!);
            await File.WriteAllTextAsync(inputPath, "v1");
            var outputPath = Path.Combine(temp.FullName, "out.cs");
            await File.WriteAllTextAsync(outputPath, "// stale");

            var layout = new TestLayout(temp.FullName);
            var context = new PipelineContext { Layout = layout, Log = TextWriter.Null, Force = false };
            var step = new TestStep("emit", ["src/in.txt"], ["out.cs"]);
            var previous = new StepResultDocument
            {
                StepId = "emit",
                Status = StepStatus.Succeeded,
                Inputs = StepFileFingerprint.HashFiles(["src/in.txt"], temp.FullName),
                Outputs = [new StepOutputRecord { Path = "out.cs", Sha256 = "deadbeef" }],
            };

            var skip = StepSkipEvaluator.ShouldSkip(step, context, previous, out _);
            await Assert.That(skip).IsFalse();
        }
        finally
        {
            temp.Delete(true);
        }
    }

    [Test]
    public async Task StepSkipEvaluator_does_not_skip_when_output_missing()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-pipeline-noskip-");
        try
        {
            var layout = new TestLayout(temp.FullName);
            var context = new PipelineContext { Layout = layout, Log = TextWriter.Null, Force = true };
            var step = new TestStep("emit", [], ["missing.cs"]);
            var skip = StepSkipEvaluator.ShouldSkip(step, context, null, out _);
            await Assert.That(skip).IsFalse();
        }
        finally
        {
            temp.Delete(true);
        }
    }

    sealed class TestLayout(string repoRoot) : IPipelineLayout
    {
        public string RepoRoot => repoRoot;
        public string StepsRoot => Path.Combine(repoRoot, "steps");
        public string ManifestDir => Path.Combine(repoRoot, "manifests");
        public string StepDir(string stepId) => Path.Combine(StepsRoot, stepId);
        public string StepArtifactsDir(string stepId) => Path.Combine(StepDir(stepId), "artifacts");
    }

    sealed class TestStep(string id, string[] inputs, string[] outputs) : IPipelineStep
    {
        public string Id => id;
        public string Description => "test";
        public IReadOnlyList<string> DependsOn => [];
        public IReadOnlyList<string> InputPaths(PipelineContext context) => inputs;
        public IReadOnlyList<string> ExpectedOutputPaths(PipelineContext context) => outputs;
        public ValueTask<StepExecutionResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new StepExecutionResult { Status = StepStatus.Succeeded });
    }
}
