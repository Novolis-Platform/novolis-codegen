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
            [new InteropImportSpec("InitWindow", NativeSignature.Create(NativeType.Void))]);

        var b = new InteropExportsFragment(
            "raylib",
            1,
            null,
            null,
            "raylib",
            EmptyPolicy,
            [new InteropStructSpec("Color", [new InteropFieldSpec("R", "byte")])],
            [new InteropImportSpec("InitWindow", NativeSignature.Create(NativeType.Void))]);

        await Assert.That(ManifestFingerprint.CanonicalText(a)).IsEqualTo(ManifestFingerprint.CanonicalText(b));
        await Assert.That(a.Sha256Hex()).IsEqualTo(b.Sha256Hex());
    }

    [Test]
    public async Task Sha256Hex_changes_when_import_set_differs()
    {
        var a = new ShimExportsFragment("shim", 1, null, null, "module.so",
            [new ShimExportSpec("A", NativeSignature.Create(NativeType.Void))]);
        var b = new ShimExportsFragment("shim", 1, null, null, "module.so",
            [new ShimExportSpec("B", NativeSignature.Create(NativeType.Void))]);
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
