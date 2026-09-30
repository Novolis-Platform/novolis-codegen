namespace Novolis.CodeGen.Bindings;

/// <summary>Lookup helpers for <see cref="IBindingManifestSource"/>.</summary>
public static class ManifestSourceExtensions
{
    /// <summary>Gets a required fragment by kind and identifier.</summary>
    /// <typeparam name="TFragment">Expected fragment type.</typeparam>
    /// <param name="source">Manifest source.</param>
    /// <param name="kind">Fragment kind.</param>
    /// <param name="id">Fragment identifier.</param>
    /// <returns>The matching fragment.</returns>
    /// <exception cref="InvalidOperationException">When no matching fragment exists.</exception>
    public static TFragment GetRequired<TFragment>(
        this IBindingManifestSource source,
        FragmentKind kind,
        string id)
        where TFragment : class, IManifestFragment =>
        source.TryGet<TFragment>(kind, id)
        ?? throw new InvalidOperationException($"Missing manifest fragment '{id}' ({kind}).");

    /// <summary>Attempts to get a fragment by kind and identifier.</summary>
    /// <typeparam name="TFragment">Expected fragment type.</typeparam>
    /// <param name="source">Manifest source.</param>
    /// <param name="kind">Fragment kind.</param>
    /// <param name="id">Fragment identifier.</param>
    /// <returns>The fragment, or <see langword="null"/> when not found.</returns>
    public static TFragment? TryGet<TFragment>(
        this IBindingManifestSource source,
        FragmentKind kind,
        string id)
        where TFragment : class, IManifestFragment =>
        source.Fragments.OfType<TFragment>().FirstOrDefault(f => f.Kind == kind && f.Id == id);
}
