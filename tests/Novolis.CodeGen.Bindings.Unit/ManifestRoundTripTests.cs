namespace Novolis.CodeGen.Bindings.Unit;

public sealed class ManifestRoundTripTests
{
    private static string FixtureDir()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "fixtures", "raylib6");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Missing fixtures at {dir}");
        return dir;
    }

    [Test]
    public async Task Interop_exports_round_trip_semantically()
    {
        var path = Path.Combine(FixtureDir(), "raylib-exports.manifest.json");
        var original = InteropExportsSerializer.LoadFromFile(path);
        var roundTrip = InteropExportsSerializer.LoadFromUtf8Bytes(original.ToUtf8Bytes());
        await Assert.That(ManifestSemanticEquality.InteropEquals(original, roundTrip)).IsTrue();
        await Assert.That(original.Imports.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Imgui_shim_round_trip_semantically()
    {
        var path = Path.Combine(FixtureDir(), "imgui-exports.manifest.json");
        var original = ShimExportsSerializer.LoadFromFile(path, "imgui", "novolis_imgui");
        var roundTrip = ShimExportsSerializer.LoadFromUtf8Bytes(original.ToUtf8Bytes(), "imgui", "novolis_imgui");
        await Assert.That(ManifestSemanticEquality.ShimEquals(original, roundTrip)).IsTrue();
    }

    [Test]
    public async Task Debug_config_round_trip_semantically()
    {
        var path = Path.Combine(FixtureDir(), "raylib-debug.manifest.json");
        var original = DebugConfigSerializer.LoadFromFile(path);
        var roundTrip = DebugConfigSerializer.LoadFromUtf8Bytes(original.ToUtf8Bytes());
        await Assert.That(ManifestSemanticEquality.DebugEquals(original, roundTrip)).IsTrue();
    }

    [Test]
    public async Task Facades_round_trip_preserves_type_count()
    {
        var path = Path.Combine(FixtureDir(), "facades.manifest.json");
        var original = FacadeTypesSerializer.LoadFromFile(path, "facades");
        var roundTrip = FacadeTypesSerializer.LoadFromUtf8Bytes(original.ToUtf8Bytes(), "facades", "facades.manifest.json");
        await Assert.That(original.Types.Count).IsEqualTo(roundTrip.Types.Count);
    }

    [Test]
    public async Task Hud_manifest_loads()
    {
        var path = Path.Combine(FixtureDir(), "hud.manifest.json");
        var fragment = FacadeTypesSerializer.LoadFromFile(path, "hud");
        await Assert.That(fragment.Types.Count).IsEqualTo(1);
        await Assert.That(fragment.Types[0].Name).IsEqualTo("Hud");
    }
}

public sealed class PipelineKernelTests
{
    [Test]
    public async Task Step_skip_when_inputs_unchanged()
    {
        var layout = new FakeLayout();
        var step = new FakeSuccessStep("step_test");
        var context = new Novolis.CodeGen.Pipeline.PipelineContext
        {
            Layout = layout,
            Log = TextWriter.Null,
            Force = false,
        };

        var previous = new Novolis.CodeGen.Pipeline.StepResultDocument
        {
            StepId = step.Id,
            Status = Novolis.CodeGen.Pipeline.StepStatus.Succeeded,
            Inputs = Novolis.CodeGen.Pipeline.StepFileFingerprint.HashFiles(step.InputPaths(context), layout.RepoRoot),
        };

        var skip = Novolis.CodeGen.Pipeline.StepSkipEvaluator.ShouldSkip(step, context, previous, out _);
        await Assert.That(skip).IsTrue();
    }

    private sealed class FakeLayout : Novolis.CodeGen.Pipeline.IPipelineLayout
    {
        public string RepoRoot { get; } = Path.GetTempPath();
        public string StepsRoot => Path.Combine(RepoRoot, "steps");
        public string ManifestDir => Path.Combine(RepoRoot, "manifests");
        public string StepDir(string stepId) => Path.Combine(StepsRoot, stepId);
        public string StepArtifactsDir(string stepId) => Path.Combine(StepDir(stepId), "artifacts");
    }

    private sealed class FakeSuccessStep(string id) : Novolis.CodeGen.Pipeline.IPipelineStep
    {
        public string Id => id;
        public string Description => "fake";
        public IReadOnlyList<string> DependsOn => [];
        public IReadOnlyList<string> InputPaths(Novolis.CodeGen.Pipeline.PipelineContext context) => [];
        public IReadOnlyList<string> ExpectedOutputPaths(Novolis.CodeGen.Pipeline.PipelineContext context) => [];
        public ValueTask<Novolis.CodeGen.Pipeline.StepExecutionResult> ExecuteAsync(
            Novolis.CodeGen.Pipeline.PipelineContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new Novolis.CodeGen.Pipeline.StepExecutionResult
            {
                Status = Novolis.CodeGen.Pipeline.StepStatus.Succeeded,
            });
    }
}
