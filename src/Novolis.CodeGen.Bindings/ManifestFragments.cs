namespace Novolis.CodeGen.Bindings;

public interface IManifestFragment
{
    string Id { get; }

    FragmentKind Kind { get; }

    string FileName { get; }

    byte[] ToUtf8Bytes();

    string Sha256Hex();
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

    public string FileName => "raylib-exports.manifest.json";

    public byte[] ToUtf8Bytes() => InteropExportsSerializer.SerializeToUtf8Bytes(this);

    public string Sha256Hex() => ManifestHashing.Sha256Hex(ToUtf8Bytes());
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

    public string FileName => Id switch
    {
        "imgui" => "imgui-exports.manifest.json",
        "raygui" => "raygui-exports.manifest.json",
        _ => $"{Id}-exports.manifest.json",
    };

    public byte[] ToUtf8Bytes() => ShimExportsSerializer.SerializeToUtf8Bytes(this);

    public string Sha256Hex() => ManifestHashing.Sha256Hex(ToUtf8Bytes());
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

    public string FileName => "raylib-debug.manifest.json";

    public byte[] ToUtf8Bytes() => DebugConfigSerializer.SerializeToUtf8Bytes(this);

    public string Sha256Hex() => ManifestHashing.Sha256Hex(ToUtf8Bytes());
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

    public string FileName => Id switch
    {
        "facades" => "facades.manifest.json",
        "hud" => "hud.manifest.json",
        "gui" => "gui.manifest.json",
        "raygui" => "raygui.manifest.json",
        _ => $"{Id}.manifest.json",
    };

    public byte[] ToUtf8Bytes() => FacadeTypesSerializer.SerializeToUtf8Bytes(this);

    public string Sha256Hex() => ManifestHashing.Sha256Hex(ToUtf8Bytes());
}

public static class ManifestHashing
{
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
}
