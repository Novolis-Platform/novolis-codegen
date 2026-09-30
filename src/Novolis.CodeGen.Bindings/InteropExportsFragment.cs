namespace Novolis.CodeGen.Bindings;

/// <summary>Manifest fragment describing LibraryImport interop exports for one native DLL.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Header">Optional file header comment.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="DllName">Native library name.</param>
/// <param name="Policy">Marshalling policy.</param>
/// <param name="Structs">Struct definitions referenced by imports.</param>
/// <param name="Imports">Import entries.</param>
/// <param name="Usings">Additional using directives emitted after interop infrastructure usings.</param>
public sealed record InteropExportsFragment(
    string Id,
    int SchemaVersion,
    string? Header,
    string? Description,
    string DllName,
    InteropPolicySpec Policy,
    IReadOnlyList<InteropStructSpec> Structs,
    IReadOnlyList<InteropImportSpec> Imports,
    IReadOnlyList<string>? Usings = null) : IManifestFragment
{
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.InteropExports;
}
