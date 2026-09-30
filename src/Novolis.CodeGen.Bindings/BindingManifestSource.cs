namespace Novolis.CodeGen.Bindings;

/// <summary>In-memory manifest source built from explicit fragments.</summary>
public sealed class BindingManifestSource : IBindingManifestSource
{
    private BindingManifestSource(IReadOnlyList<IManifestFragment> fragments) =>
        Fragments = fragments;

    /// <inheritdoc />
    public IReadOnlyList<IManifestFragment> Fragments { get; }

    /// <summary>Creates a source from an array of fragments.</summary>
    /// <param name="fragments">Manifest fragments.</param>
    /// <returns>A configured source.</returns>
    public static BindingManifestSource Create(params IManifestFragment[] fragments) =>
        new(fragments);

    /// <summary>Creates a source from a sequence of fragments.</summary>
    /// <param name="fragments">Manifest fragments.</param>
    /// <returns>A configured source.</returns>
    public static BindingManifestSource Create(IEnumerable<IManifestFragment> fragments) =>
        new(fragments.ToList());
}
