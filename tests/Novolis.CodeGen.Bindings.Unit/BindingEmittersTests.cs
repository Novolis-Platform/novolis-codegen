using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class BindingEmittersTests
{
    private static readonly InteropPolicySpec DisabledMarshallingPolicy =
        new([], [], "AggressiveInlining", true);

    [Test]
    public async Task LibraryImportEmitter_preserves_typed_bool_utf8_and_out_signatures()
    {
        var fragment = new InteropExportsFragment(
            "native",
            1,
            null,
            null,
            "native",
            DisabledMarshallingPolicy,
            [new InteropStructSpec("NativeImage", [new InteropFieldSpec("Data", "nint")])],
            [
                new InteropImportSpec(
                    "Export",
                    NativeSignature.Create(
                        NativeType.NativeInt,
                        new NativeParameter("image", NativeType.Named("NativeImage")),
                        new NativeParameter("fileType", NativeType.Utf8String),
                        new NativeParameter("fileSize", NativeType.Int32, NativeParameterModifier.Out))),
                new InteropImportSpec("Ready", NativeSignature.Create(NativeType.Boolean)),
            ],
            ["Example.Interop"]);
        var context = CreateContext(fragment);
        var target = new EmitTarget(
            "NativeExports",
            EmitStrategy.LibraryImport,
            "generated/NativeExports.g.cs",
            "Example.Interop",
            "Example.Bindings",
            LibraryConstantName: "NativeDll",
            TypeSummary: "Native <c>LibraryImport</c> surface.");

        var output = new LibraryImportEmitter().Emit(new EmitRequest(fragment, "hash", target, context));

        await Assert.That(output).Contains("private const string NativeDll = \"native\"");
        await Assert.That(output).Contains("[return: MarshalAs(UnmanagedType.I1)]");
        await Assert.That(output).Contains("[MarshalUsing(typeof(Utf8StringMarshaller))] string fileType");
        await Assert.That(output).Contains("out int fileSize");
        await Assert.That(output).Contains("internal struct NativeImage");
    }

    [Test]
    public async Task DynamicExportsEmitter_emits_embedded_struct_and_function_pointer()
    {
        var fragment = new ShimExportsFragment(
            "shim",
            1,
            null,
            null,
            "shim",
            [
                new ShimExportSpec(
                    "Draw",
                    NativeSignature.Create(
                        NativeType.Int32,
                        new NativeParameter("bounds", NativeType.Named("Rectangle")),
                        new NativeParameter("text", NativeType.Utf8String))),
            ],
            [new EmbeddedTypeSpec("Rectangle", [new EmbeddedFieldSpec("X", "float")])]);
        var context = CreateContext(fragment);
        var target = new EmitTarget(
            "ShimExports",
            EmitStrategy.DynamicExports,
            "generated/ShimExports.g.cs",
            "Example.Interop",
            "Example.Bindings");

        var output = new DynamicExportsEmitter().Emit(new EmitRequest(fragment, "hash", target, context));

        await Assert.That(output).Contains("internal struct Rectangle");
        await Assert.That(output).Contains("delegate* unmanaged<Rectangle, byte*, int> Draw_ptr");
        await Assert.That(output).Contains("NativeLibrary.GetExport(module, \"Draw\")");
    }

    [Test]
    public async Task FacadeForwardEmitter_emits_documentation_before_inlined_forward()
    {
        var fragment = new FacadeTypesFragment(
            "facades",
            [
                new FacadeTypeSpec(
                    "Graphics",
                    "Example.Runtime",
                    "Graphics",
                    "Drawing façade.",
                    ["Example.Interop"],
                    [new FacadeMethodSpec("Begin", "void Begin()", "Native.Begin()", "Begin drawing.")]),
            ]);
        var context = CreateContext(fragment);
        var target = new EmitTarget(
            "Graphics",
            EmitStrategy.FacadeForward,
            "generated/Graphics.g.cs",
            "Example.Runtime",
            "Example.Runtime",
            FacadeMethodImpl: "AggressiveInlining");

        var output = new FacadeForwardEmitter().Emit(new EmitRequest(fragment, "hash", target, context));

        var summaryIndex = output.IndexOf("/// Begin drawing.", StringComparison.Ordinal);
        var inlineIndex = output.IndexOf("[MethodImpl(MethodImplOptions.AggressiveInlining)]", StringComparison.Ordinal);
        await Assert.That(summaryIndex).IsLessThan(inlineIndex);
        await Assert.That(output).Contains("public static void Begin() => Native.Begin();");
    }

    [Test]
    public async Task BindingCodegenHost_skips_optional_jobs_and_writes_selected_output()
    {
        var root = TestPaths.Root("binding-host");
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), root);
        var fragment = new InteropExportsFragment(
            "native",
            1,
            null,
            null,
            "native",
            DisabledMarshallingPolicy,
            [],
            [new InteropImportSpec("Ready", NativeSignature.Create(NativeType.Boolean))]);
        var source = BindingManifestSource.Create(fragment);
        var project = BindingProject.Create("example")
            .AddJob(
                new BindingEmitJob(
                    "core",
                    FragmentKind.InteropExports,
                    "native",
                    new LibraryImportEmitter(),
                    new EmitTarget(
                        "NativeExports",
                        EmitStrategy.LibraryImport,
                        "generated/NativeExports.g.cs",
                        "Example.Interop",
                        "Example.Bindings",
                        LibraryConstantName: "NativeDll")))
            .AddJob(
                new BindingEmitJob(
                    "optional",
                    FragmentKind.InteropExports,
                    "native",
                    new LibraryImportEmitter(),
                    new EmitTarget(
                        "OptionalExports",
                        EmitStrategy.LibraryImport,
                        "generated/OptionalExports.g.cs",
                        "Example.Interop",
                        "Example.Bindings",
                        LibraryConstantName: "NativeDll"),
                    Optional: true));
        var options = new BindingCodegenOptions
        {
            Environment = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = root },
            Manifests = source,
            RegenerateHint = "dotnet run --project example",
        };
        var run = new BindingCodegenRun<TestPhase, TestContext>
        {
            Project = project,
            Options = options,
            SelectPhase = _ => TestPhase.Emit,
            CreateContext = (_, manifest, outputPath, fingerprint) => new TestContext
            {
                Environment = options.Environment,
                OutputPath = outputPath,
                Fragment = manifest,
                ManifestSha256 = fingerprint,
                RegenerateHint = options.RegenerateHint,
            },
        };

        var exit = new BindingCodegenHost<TestPhase, TestContext>().Generate(run);

        await Assert.That(exit).IsEqualTo(0);
        await Assert.That(fileSystem.File.Exists(TestPaths.Combine(root, "generated", "NativeExports.g.cs"))).IsTrue();
        await Assert.That(fileSystem.File.Exists(TestPaths.Combine(root, "generated", "OptionalExports.g.cs"))).IsFalse();
    }

    private static BindingEmitContext CreateContext(IManifestFragment fragment) =>
        new()
        {
            Environment = CodegenEnvironment.Physical(TestPaths.Root("binding-emitters")),
            OutputPath = "generated/output.cs",
            Fragment = fragment,
            ManifestSha256 = "hash",
            RegenerateHint = "dotnet run --project example",
        };

    private enum TestPhase
    {
        Emit,
    }

    private sealed class TestContext : BindingEmitContext;
}
