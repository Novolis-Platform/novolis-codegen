using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class BindingCodegenHostTests
{
    static readonly InteropPolicySpec EmptyPolicy = new([], [], null, false);

    [Test]
    public async Task BindingCodegenOptions_Physical_sets_defaults()
    {
        const string repoRoot = @"C:\novolis\binding-options";
        var fragment = new InteropExportsFragment("raylib", 1, null, null, "raylib", EmptyPolicy, [], []);
        var source = BindingManifestSource.Create(fragment);
        var options = BindingCodegenOptions.Physical(repoRoot, source);

        await Assert.That(options.IncludeRaygui).IsTrue();
        await Assert.That(options.VerifyManifest).IsTrue();
        await Assert.That(options.RegenerateHint).Contains("Novolis.Raylib.Pipeline");
        await Assert.That(options.Environment.RepoRoot).IsEqualTo(repoRoot);
        await Assert.That(options.Manifests).IsSameReferenceAs(source);
    }

    [Test]
    public async Task EmitRequest_and_BindingEmitContext_round_trip()
    {
        const string repoRoot = @"C:\novolis\emit-request";
        var fragment = new InteropExportsFragment("raylib", 1, null, null, "raylib", EmptyPolicy, [], []);
        var env = CodegenEnvironment.Physical(repoRoot);
        var context = new BindingEmitContext
        {
            Environment = env,
            OutputPath = @"generated\out.cs",
            Fragment = fragment,
            ManifestSha256 = "deadbeef",
            RegenerateHint = "regen hint",
        };
        var target = new EmitTarget("Gen", EmitStrategy.LibraryImport, "out.cs", "Ns", "Asm");
        var request = new EmitRequest(fragment, "deadbeef", target, context);

        await Assert.That(request.Fragment).IsSameReferenceAs(fragment);
        await Assert.That(request.ManifestSha256).IsEqualTo("deadbeef");
        await Assert.That(request.Target.ClassName).IsEqualTo("Gen");
        await Assert.That(context.RepoRoot).IsEqualTo(repoRoot);
        await Assert.That(context.Fragment.Id).IsEqualTo("raylib");

        var emitter = new StubEmitter();
        await Assert.That(emitter.Emit(request)).IsEqualTo("// stub");
    }

    sealed class StubEmitter : IBindingEmitter
    {
        public EmitStrategy Strategy => EmitStrategy.LibraryImport;
        public string Emit(EmitRequest request) => "// stub";
    }
}
