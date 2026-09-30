namespace Novolis.CodeGen.Bindings;

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
