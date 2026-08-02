using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class ManifestFingerprintTests
{
    static readonly InteropPolicySpec EmptyPolicy = new([], [], null, false);

    [Test]
    public async Task CanonicalText_is_order_independent_for_structs_and_imports()
    {
        var a = new InteropExportsFragment(
            "raylib",
            1,
            null,
            null,
            "raylib",
            EmptyPolicy,
            [new InteropStructSpec("Color", [new InteropFieldSpec("R", "byte")])],
            [new InteropImportSpec("InitWindow", InteropTemplate.VoidVoid)]);

        var b = new InteropExportsFragment(
            "raylib",
            1,
            null,
            null,
            "raylib",
            EmptyPolicy,
            [new InteropStructSpec("Color", [new InteropFieldSpec("R", "byte")])],
            [new InteropImportSpec("InitWindow", InteropTemplate.VoidVoid)]);

        await Assert.That(ManifestFingerprint.CanonicalText(a)).IsEqualTo(ManifestFingerprint.CanonicalText(b));
        await Assert.That(a.Sha256Hex()).IsEqualTo(b.Sha256Hex());
    }

    [Test]
    public async Task Sha256Hex_changes_when_import_set_differs()
    {
        var a = new ShimExportsFragment("shim", 1, null, null, "module.so",
            [new ShimExportSpec("A", "void_void")]);
        var b = new ShimExportsFragment("shim", 1, null, null, "module.so",
            [new ShimExportSpec("B", "void_void")]);
        await Assert.That(a.Sha256Hex()).IsNotEqualTo(b.Sha256Hex());
    }

    [Test]
    public async Task CanonicalText_covers_debug_and_facade_fragments()
    {
        var debug = new DebugConfigFragment(
            "dbg", 1, null, "Notify", "FrameNotify", "CAPTURE", "PNG",
            new DebugSymbolMapSpec("Load", "Export", "Unload", "Free"));
        await Assert.That(ManifestFingerprint.CanonicalText(debug)).StartsWith("debug|dbg|");

        var facade = new FacadeTypesFragment("facade",
        [
            new FacadeTypeSpec(
                "Raylib",
                "Novolis.Raylib",
                "Generated",
                "summary",
                ["System"],
                [new FacadeMethodSpec("Draw", "void Draw()", "Draw();")]),
        ]);
        await Assert.That(ManifestFingerprint.CanonicalText(facade)).Contains("type:Raylib");
    }

    [Test]
    public async Task Unsupported_fragment_throws()
    {
        var fake = new FakeFragment("x");
        await Assert.That(() => ManifestFingerprint.CanonicalText(fake)).Throws<NotSupportedException>();
    }

    sealed class FakeFragment(string id) : IManifestFragment
    {
        public string Id => id;
        public FragmentKind Kind => FragmentKind.NativeArtifacts;
    }
}

public sealed class ManifestSemanticEqualityTests
{
    [Test]
    public async Task InteropEquals_ignores_import_order()
    {
        var a = new InteropExportsFragment("id", 1, null, null, "dll",
            new InteropPolicySpec([], [], null, false), [],
            [new InteropImportSpec("A", "t"), new InteropImportSpec("B", "t")]);
        var b = new InteropExportsFragment("id", 1, null, null, "dll",
            new InteropPolicySpec([], [], null, false), [],
            [new InteropImportSpec("B", "t"), new InteropImportSpec("A", "t")]);
        await Assert.That(ManifestSemanticEquality.InteropEquals(a, b)).IsTrue();
    }

    [Test]
    public async Task ShimEquals_and_debug_and_facade()
    {
        var shimA = new ShimExportsFragment("s", 1, null, null, "m.so", [new ShimExportSpec("X", "t")]);
        var shimB = new ShimExportsFragment("s", 1, null, null, "m.so", [new ShimExportSpec("X", "t")]);
        await Assert.That(ManifestSemanticEquality.ShimEquals(shimA, shimB)).IsTrue();

        var sym = new DebugSymbolMapSpec("L", "E", "U", "F");
        var dbgA = new DebugConfigFragment("d", 1, null, "N", "F", "C", "P", sym);
        var dbgB = new DebugConfigFragment("d", 1, null, "N", "F", "C", "P", sym);
        await Assert.That(ManifestSemanticEquality.DebugEquals(dbgA, dbgB)).IsTrue();

        var facadeA = new FacadeTypesFragment("f",
            [new FacadeTypeSpec("T", "Ns", "Dir", null, [], [new FacadeMethodSpec("M", "sig", "body")])]);
        var facadeB = new FacadeTypesFragment("f",
            [new FacadeTypeSpec("T", "Ns", "Dir", null, [], [new FacadeMethodSpec("M", "sig", "body")])]);
        await Assert.That(ManifestSemanticEquality.FacadeEquals(facadeA, facadeB)).IsTrue();
    }

    [Test]
    public async Task ManifestSourceExtensions_get_required_and_try()
    {
        var interop = new InteropExportsFragment("raylib", 1, null, null, "raylib",
            new InteropPolicySpec([], [], null, false), [], []);
        var source = BindingManifestSource.Create(interop);

        var found = source.TryGet<InteropExportsFragment>(FragmentKind.InteropExports, "raylib");
        await Assert.That(found).IsNotNull();

        var required = source.GetRequired<InteropExportsFragment>(FragmentKind.InteropExports, "raylib");
        await Assert.That(required.DllName).IsEqualTo("raylib");

        await Assert.That(() => source.GetRequired<InteropExportsFragment>(FragmentKind.InteropExports, "missing"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ManifestHashing_sha256_hex()
    {
        var hex = ManifestHashing.Sha256Hex("hello"u8.ToArray());
        await Assert.That(hex.Length).IsEqualTo(64);
        await Assert.That(hex).IsEqualTo(ManifestHashing.Sha256Hex("hello"u8.ToArray()));
    }

    [Test]
    public async Task ManifestFingerprint_includes_interop_policy_fields()
    {
        var policy = new InteropPolicySpec(
            ["void_void"],
            ["InitWindow"],
            "AggressiveInlining",
            true);
        var fragment = new InteropExportsFragment("raylib", 1, null, null, "raylib", policy, [], []);
        var text = ManifestFingerprint.CanonicalText(fragment);
        await Assert.That(text).Contains("suppressTemplate=void_void");
        await Assert.That(text).Contains("neverSuppress=InitWindow");
        await Assert.That(text).Contains("facadeImpl=AggressiveInlining");
        await Assert.That(text).Contains("disableMarshalling=True");
    }

    [Test]
    public async Task BindingManifestSource_Create_from_enumerable()
    {
        var a = new ShimExportsFragment("s1", 1, null, null, "m.so", []);
        var b = new DebugConfigFragment("d", 1, null, "N", "F", "C", "P",
            new DebugSymbolMapSpec("L", "E", "U", "F"));
        var source = BindingManifestSource.Create(new IManifestFragment[] { a, b });
        await Assert.That(source.TryGet<ShimExportsFragment>(FragmentKind.ShimExports, "s1")).IsNotNull();
        await Assert.That(source.TryGet<DebugConfigFragment>(FragmentKind.DebugConfig, "d")).IsNotNull();
    }

    [Test]
    public async Task FacadeTypesFragment_exposes_kind()
    {
        var fragment = new FacadeTypesFragment("facade", []);
        await Assert.That(fragment.Kind).IsEqualTo(FragmentKind.FacadeTypes);
    }
}

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

        var without = BindingCodegenExecutor.FilterJobs(project, includeRaygui: false).ToList();
        await Assert.That(without.Count).IsEqualTo(1);
        await Assert.That(without[0].Label).IsEqualTo("core");

        var with = BindingCodegenExecutor.FilterJobs(project, includeRaygui: true).ToList();
        await Assert.That(with.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ValidateCompanions_throws_when_missing()
    {
        const string repoRoot = @"C:\novolis\binding-test";
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem(new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>(), repoRoot);
        var env = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = repoRoot };
        var project = BindingProject.Create("test").RequireCompanion("missing.txt", "required");

        await Assert.That(() => BindingCodegenExecutor.ValidateCompanions(project, env))
            .Throws<FileNotFoundException>();
    }

    [Test]
    public async Task ValidateCompanions_passes_when_present()
    {
        const string repoRoot = @"C:\novolis\binding-test-ok";
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem(
            new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>
            {
                [@"C:\novolis\binding-test-ok\companion.txt"] = new("ok"),
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
