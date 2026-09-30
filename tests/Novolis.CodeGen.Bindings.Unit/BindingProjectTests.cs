using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class BindingProjectTests
{
    [Test]
    public async Task FilterJobs_respects_optional_raygui_flag()
    {
        var emitter = new StubEmitter();
        var target = new EmitTarget("Gen", EmitStrategy.LibraryImport, "out.cs", "Ns", "Asm");
        var project = BindingProject.Create("test")
            .AddJob(new BindingEmitJob("core", FragmentKind.InteropExports, "raylib", emitter, target))
            .AddJob(new BindingEmitJob("raygui", FragmentKind.InteropExports, "raygui", emitter, target, Optional: true));

        var without = BindingCodegenExecutor.FilterJobs(project, includeOptional: false).ToList();
        await Assert.That(without.Count).IsEqualTo(1);
        await Assert.That(without[0].Label).IsEqualTo("core");

        var with = BindingCodegenExecutor.FilterJobs(project, includeOptional: true).ToList();
        await Assert.That(with.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ValidateCompanions_throws_when_missing()
    {
        var repoRoot = TestPaths.Root("binding-test");
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem(new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>(), repoRoot);
        var env = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = repoRoot };
        var project = BindingProject.Create("test").RequireCompanion("missing.txt", "required");

        await Assert.That(() => BindingCodegenExecutor.ValidateCompanions(project, env))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task ValidateCompanions_passes_when_present()
    {
        var repoRoot = TestPaths.Root("binding-test-ok");
        var companion = TestPaths.Combine(repoRoot, "companion.txt");
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
            new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>
            {
                [companion] = new("ok"),
            },
            repoRoot);
        var env = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = repoRoot };
        var project = BindingProject.Create("test").RequireCompanion("companion.txt", "required");

        BindingCodegenExecutor.ValidateCompanions(project, env);
        await Assert.That(env.FileExists("companion.txt")).IsTrue();
    }

    sealed class StubEmitter : IBindingEmitter
    {
        public EmitStrategy Strategy => EmitStrategy.LibraryImport;
        public string Emit(EmitRequest request) => "// stub";
    }
}
