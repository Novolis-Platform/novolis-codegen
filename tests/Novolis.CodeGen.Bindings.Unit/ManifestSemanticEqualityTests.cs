using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class ManifestSemanticEqualityTests
{
    [Test]
    public async Task InteropEquals_ignores_import_order()
    {
        var a = new InteropExportsFragment("id", 1, null, null, "dll",
            new InteropPolicySpec([], [], null, false), [],
            [new InteropImportSpec("A", NativeSignature.Create(NativeType.Void)), new InteropImportSpec("B", NativeSignature.Create(NativeType.Void))]);
        var b = new InteropExportsFragment("id", 1, null, null, "dll",
            new InteropPolicySpec([], [], null, false), [],
            [new InteropImportSpec("B", NativeSignature.Create(NativeType.Void)), new InteropImportSpec("A", NativeSignature.Create(NativeType.Void))]);
        await Assert.That(ManifestSemanticEquality.InteropEquals(a, b)).IsTrue();
    }

    [Test]
    public async Task ShimEquals_and_debug_and_facade()
    {
        var shimA = new ShimExportsFragment("s", 1, null, null, "m.so", [new ShimExportSpec("X", NativeSignature.Create(NativeType.Void))]);
        var shimB = new ShimExportsFragment("s", 1, null, null, "m.so", [new ShimExportSpec("X", NativeSignature.Create(NativeType.Void))]);
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
            ["InitWindow"],
            ["InitWindow"],
            "AggressiveInlining",
            true);
        var fragment = new InteropExportsFragment("raylib", 1, null, null, "raylib", policy, [], []);
        var text = ManifestFingerprint.CanonicalText(fragment);
        await Assert.That(text).Contains("suppressFunction=InitWindow");
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
