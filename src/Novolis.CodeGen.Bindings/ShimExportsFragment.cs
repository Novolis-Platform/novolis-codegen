namespace Novolis.CodeGen.Bindings;

/// <summary>Manifest fragment describing dynamic shim exports loaded from a native module.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Header">Optional file header comment.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="ModuleFileName">Native module file name.</param>
/// <param name="Exports">Export entries.</param>
/// <param name="EmbeddedTypes">Blittable types emitted inside the dynamic-export class.</param>
public sealed record ShimExportsFragment(
    string Id,
    int SchemaVersion,
    string? Header,
    string? Description,
    string ModuleFileName,
    IReadOnlyList<ShimExportSpec> Exports,
    IReadOnlyList<EmbeddedTypeSpec>? EmbeddedTypes = null) : IManifestFragment
{
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.ShimExports;
}
