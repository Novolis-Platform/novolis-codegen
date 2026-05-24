namespace Novolis.CodeGen.Bindings;

/// <summary>Provides binding manifest fragments consumed by codegen emitters.</summary>
public interface IBindingManifestSource
{
    /// <summary>All manifest fragments available to the host.</summary>
    IReadOnlyList<IManifestFragment> Fragments { get; }
}

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

/// <summary>Fingerprint helpers for manifest fragments.</summary>
public static class ManifestFragmentExtensions
{
    /// <summary>Computes the SHA-256 hex fingerprint for <paramref name="fragment"/>.</summary>
    /// <param name="fragment">Manifest fragment.</param>
    /// <returns>64-character lowercase hex digest.</returns>
    public static string Sha256Hex(this IManifestFragment fragment) =>
        ManifestFingerprint.Sha256Hex(fragment);
}
