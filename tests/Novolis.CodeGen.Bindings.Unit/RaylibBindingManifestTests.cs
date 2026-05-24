using Novolis.CodeGen.Bindings;
using Novolis.Raylib.Manifests;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class RaylibBindingManifestTests
{
    [Test]
    public async Task Raylib6_interop_manifest_has_imports()
    {
        var interop = RaylibBindingManifestSource.Instance.GetRequired<InteropExportsFragment>(
            FragmentKind.InteropExports,
            "raylib6");
        await Assert.That(interop.Imports.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Manifest_fingerprints_are_stable()
    {
        var interop = RaylibBindingManifestSource.Instance.GetRequired<InteropExportsFragment>(
            FragmentKind.InteropExports,
            "raylib6");
        var first = interop.Sha256Hex();
        var second = interop.Sha256Hex();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task Facades_manifest_loads_types()
    {
        var facades = RaylibBindingManifestSource.Instance.GetRequired<FacadeTypesFragment>(
            FragmentKind.FacadeTypes,
            "facades");
        await Assert.That(facades.Types.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Hud_manifest_has_hud_type()
    {
        var hud = RaylibBindingManifestSource.Instance.GetRequired<FacadeTypesFragment>(
            FragmentKind.FacadeTypes,
            "hud");
        await Assert.That(hud.Types[0].Name).IsEqualTo("Hud");
    }
}
