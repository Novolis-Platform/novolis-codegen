namespace Novolis.CodeGen.Bindings;

/// <summary>Semantic equality checks for manifest fragments (ignores ordering differences).</summary>
public static class ManifestSemanticEquality
{
    /// <summary>Compares interop fragments by DLL name and import set.</summary>
    /// <param name="a">First fragment.</param>
    /// <param name="b">Second fragment.</param>
    /// <returns><see langword="true"/> when semantically equal.</returns>
    public static bool InteropEquals(InteropExportsFragment a, InteropExportsFragment b) =>
        a.DllName == b.DllName &&
        a.Imports.Count == b.Imports.Count &&
        a.Imports.OrderBy(i => i.Name).SequenceEqual(b.Imports.OrderBy(i => i.Name));

    /// <summary>Compares shim fragments by export set.</summary>
    /// <param name="a">First fragment.</param>
    /// <param name="b">Second fragment.</param>
    /// <returns><see langword="true"/> when semantically equal.</returns>
    public static bool ShimEquals(ShimExportsFragment a, ShimExportsFragment b) =>
        a.Exports.Count == b.Exports.Count &&
        a.Exports.OrderBy(e => e.Export).SequenceEqual(b.Exports.OrderBy(e => e.Export));

    /// <summary>Compares debug fragments by hook names and symbol map.</summary>
    /// <param name="a">First fragment.</param>
    /// <param name="b">Second fragment.</param>
    /// <returns><see langword="true"/> when semantically equal.</returns>
    public static bool DebugEquals(DebugConfigFragment a, DebugConfigFragment b) =>
        a.NotifyAfterNativeCall == b.NotifyAfterNativeCall &&
        a.FrameHubNotifyAfter == b.FrameHubNotifyAfter &&
        a.Symbols.LoadImageFromScreen == b.Symbols.LoadImageFromScreen;

    /// <summary>Compares façade fragments by type and method names.</summary>
    /// <param name="a">First fragment.</param>
    /// <param name="b">Second fragment.</param>
    /// <returns><see langword="true"/> when semantically equal.</returns>
    public static bool FacadeEquals(FacadeTypesFragment a, FacadeTypesFragment b) =>
        a.Id == b.Id && a.Types.Count == b.Types.Count &&
        a.Types.Zip(b.Types).All(pair => pair.First.Name == pair.Second.Name &&
                                         pair.First.Methods.Count == pair.Second.Methods.Count);
}
