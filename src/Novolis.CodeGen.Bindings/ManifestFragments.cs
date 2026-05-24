namespace Novolis.CodeGen.Bindings;

public interface IManifestFragment
{
    string Id { get; }

    FragmentKind Kind { get; }
}

public sealed record InteropPolicySpec(
    IReadOnlyList<string> SuppressGcTransitionByTemplate,
    IReadOnlyList<string> NeverSuppressGcTransition,
    string? FacadeMethodImpl,
    bool UseDisableRuntimeMarshalling);

public sealed record InteropStructSpec(string Name, IReadOnlyList<InteropFieldSpec> Fields);

public sealed record InteropFieldSpec(string Name, string ClrType);

public sealed record InteropImportSpec(
    string Name,
    string Template,
    string? Description = null,
    bool? SuppressGcTransition = null);

public sealed record InteropExportsFragment(
    string Id,
    int SchemaVersion,
    string? Header,
    string? Description,
    string DllName,
    InteropPolicySpec Policy,
    IReadOnlyList<InteropStructSpec> Structs,
    IReadOnlyList<InteropImportSpec> Imports) : IManifestFragment
{
    public FragmentKind Kind => FragmentKind.InteropExports;
}

public sealed record ShimExportSpec(string Export, string Template);

public sealed record ShimExportsFragment(
    string Id,
    int SchemaVersion,
    string? Header,
    string? Description,
    string ModuleFileName,
    IReadOnlyList<ShimExportSpec> Exports) : IManifestFragment
{
    public FragmentKind Kind => FragmentKind.ShimExports;
}

public sealed record DebugSymbolMapSpec(
    string LoadImageFromScreen,
    string ExportImageToMemory,
    string UnloadImage,
    string MemFree);

public sealed record DebugConfigFragment(
    string Id,
    int SchemaVersion,
    string? Description,
    string NotifyAfterNativeCall,
    string FrameHubNotifyAfter,
    string CaptureEnvVar,
    string CapturePngFileType,
    DebugSymbolMapSpec Symbols) : IManifestFragment
{
    public FragmentKind Kind => FragmentKind.DebugConfig;
}

public sealed record FacadeMethodSpec(
    string Name,
    string Signature,
    string Body,
    string? Summary = null);

public sealed record FacadeTypeSpec(
    string Name,
    string Namespace,
    string Folder,
    string? TypeSummary,
    IReadOnlyList<string> Usings,
    IReadOnlyList<FacadeMethodSpec> Methods);

public sealed record FacadeTypesFragment(
    string Id,
    IReadOnlyList<FacadeTypeSpec> Types) : IManifestFragment
{
    public FragmentKind Kind => FragmentKind.FacadeTypes;
}

public static class ManifestHashing
{
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
}

public static class ManifestSemanticEquality
{
    public static bool InteropEquals(InteropExportsFragment a, InteropExportsFragment b) =>
        a.DllName == b.DllName &&
        a.Imports.Count == b.Imports.Count &&
        a.Imports.OrderBy(i => i.Name).SequenceEqual(b.Imports.OrderBy(i => i.Name));

    public static bool ShimEquals(ShimExportsFragment a, ShimExportsFragment b) =>
        a.Exports.Count == b.Exports.Count &&
        a.Exports.OrderBy(e => e.Export).SequenceEqual(b.Exports.OrderBy(e => e.Export));

    public static bool DebugEquals(DebugConfigFragment a, DebugConfigFragment b) =>
        a.NotifyAfterNativeCall == b.NotifyAfterNativeCall &&
        a.FrameHubNotifyAfter == b.FrameHubNotifyAfter &&
        a.Symbols.LoadImageFromScreen == b.Symbols.LoadImageFromScreen;

    public static bool FacadeEquals(FacadeTypesFragment a, FacadeTypesFragment b) =>
        a.Id == b.Id && a.Types.Count == b.Types.Count &&
        a.Types.Zip(b.Types).All(pair => pair.First.Name == pair.Second.Name &&
                                         pair.First.Methods.Count == pair.Second.Methods.Count);
}
