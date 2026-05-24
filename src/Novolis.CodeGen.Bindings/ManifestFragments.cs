namespace Novolis.CodeGen.Bindings;

/// <summary>Typed slice of a binding manifest (interop, shim, debug, façade, etc.).</summary>
public interface IManifestFragment
{
    /// <summary>Stable fragment identifier within its <see cref="Kind"/>.</summary>
    string Id { get; }

    /// <summary>Fragment kind.</summary>
    FragmentKind Kind { get; }
}

/// <summary>Interop marshalling policy applied when emitting LibraryImport stubs.</summary>
/// <param name="SuppressGcTransitionByTemplate">Templates for which GC transition suppression is enabled.</param>
/// <param name="NeverSuppressGcTransition">Import names that must never suppress GC transition.</param>
/// <param name="FacadeMethodImpl">Optional MethodImpl attribute text for façade methods.</param>
/// <param name="UseDisableRuntimeMarshalling">When <see langword="true"/>, emit DisableRuntimeMarshalling at assembly level.</param>
public sealed record InteropPolicySpec(
    IReadOnlyList<string> SuppressGcTransitionByTemplate,
    IReadOnlyList<string> NeverSuppressGcTransition,
    string? FacadeMethodImpl,
    bool UseDisableRuntimeMarshalling);

/// <summary>Struct layout declared in an interop manifest.</summary>
/// <param name="Name">Struct name.</param>
/// <param name="Fields">Ordered fields.</param>
public sealed record InteropStructSpec(string Name, IReadOnlyList<InteropFieldSpec> Fields);

/// <summary>One field on an interop struct.</summary>
/// <param name="Name">Field name.</param>
/// <param name="ClrType">CLR type name as emitted in source.</param>
public sealed record InteropFieldSpec(string Name, string ClrType);

/// <summary>One native import entry in an interop manifest.</summary>
/// <param name="Name">Native function name.</param>
/// <param name="Template">Template key (see <see cref="InteropTemplate"/>).</param>
/// <param name="Description">Optional XML doc summary for the generated member.</param>
/// <param name="SuppressGcTransition">Per-import GC transition override.</param>
public sealed record InteropImportSpec(
    string Name,
    string Template,
    string? Description = null,
    bool? SuppressGcTransition = null);

/// <summary>Manifest fragment describing LibraryImport interop exports for one native DLL.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Header">Optional file header comment.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="DllName">Native library name.</param>
/// <param name="Policy">Marshalling policy.</param>
/// <param name="Structs">Struct definitions referenced by imports.</param>
/// <param name="Imports">Import entries.</param>
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
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.InteropExports;
}

/// <summary>One export entry in a dynamic shim manifest.</summary>
/// <param name="Export">Native export symbol name.</param>
/// <param name="Template">Template key for the delegate signature.</param>
public sealed record ShimExportSpec(string Export, string Template);

/// <summary>Manifest fragment describing dynamic shim exports loaded from a native module.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Header">Optional file header comment.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="ModuleFileName">Native module file name.</param>
/// <param name="Exports">Export entries.</param>
public sealed record ShimExportsFragment(
    string Id,
    int SchemaVersion,
    string? Header,
    string? Description,
    string ModuleFileName,
    IReadOnlyList<ShimExportSpec> Exports) : IManifestFragment
{
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.ShimExports;
}

/// <summary>Native symbol names used by debug capture hooks.</summary>
/// <param name="LoadImageFromScreen">LoadImageFromScreen symbol.</param>
/// <param name="ExportImageToMemory">ExportImageToMemory symbol.</param>
/// <param name="UnloadImage">UnloadImage symbol.</param>
/// <param name="MemFree">MemFree symbol.</param>
public sealed record DebugSymbolMapSpec(
    string LoadImageFromScreen,
    string ExportImageToMemory,
    string UnloadImage,
    string MemFree);

/// <summary>Manifest fragment for debug capture configuration and native symbols.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="NotifyAfterNativeCall">Hook invoked after each native call when debugging.</param>
/// <param name="FrameHubNotifyAfter">Frame hub notification hook name.</param>
/// <param name="CaptureEnvVar">Environment variable enabling capture.</param>
/// <param name="CapturePngFileType">PNG file type constant for capture.</param>
/// <param name="Symbols">Native symbol map.</param>
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
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.DebugConfig;
}

/// <summary>One method on a generated façade type.</summary>
/// <param name="Name">Method name.</param>
/// <param name="Signature">Method signature (parameters and return type).</param>
/// <param name="Body">Method body source.</param>
/// <param name="Summary">Optional XML doc summary.</param>
public sealed record FacadeMethodSpec(
    string Name,
    string Signature,
    string Body,
    string? Summary = null);

/// <summary>One hand-authored façade type emitted from manifest data.</summary>
/// <param name="Name">Type name.</param>
/// <param name="Namespace">CLR namespace.</param>
/// <param name="Folder">Relative folder under the project.</param>
/// <param name="TypeSummary">Optional type-level XML summary.</param>
/// <param name="Usings">Additional using directives.</param>
/// <param name="Methods">Methods to emit.</param>
public sealed record FacadeTypeSpec(
    string Name,
    string Namespace,
    string Folder,
    string? TypeSummary,
    IReadOnlyList<string> Usings,
    IReadOnlyList<FacadeMethodSpec> Methods);

/// <summary>Manifest fragment listing façade types to generate.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="Types">Façade type specifications.</param>
public sealed record FacadeTypesFragment(
    string Id,
    IReadOnlyList<FacadeTypeSpec> Types) : IManifestFragment
{
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.FacadeTypes;
}

/// <summary>SHA-256 helpers for raw manifest bytes.</summary>
public static class ManifestHashing
{
    /// <summary>Returns lowercase hex SHA-256 of <paramref name="bytes"/>.</summary>
    /// <param name="bytes">Content to hash.</param>
    /// <returns>64-character hex digest.</returns>
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
}

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
