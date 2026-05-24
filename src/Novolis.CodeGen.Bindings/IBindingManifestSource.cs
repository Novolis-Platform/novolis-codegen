namespace Novolis.CodeGen.Bindings;

public interface IBindingManifestSource
{
    IReadOnlyList<IManifestFragment> Fragments { get; }
}

public static class ManifestSourceExtensions
{
    public static TFragment GetRequired<TFragment>(
        this IBindingManifestSource source,
        FragmentKind kind,
        string id)
        where TFragment : class, IManifestFragment =>
        source.TryGet<TFragment>(kind, id)
        ?? throw new InvalidOperationException($"Missing manifest fragment '{id}' ({kind}).");

    public static TFragment? TryGet<TFragment>(
        this IBindingManifestSource source,
        FragmentKind kind,
        string id)
        where TFragment : class, IManifestFragment =>
        source.Fragments.OfType<TFragment>().FirstOrDefault(f => f.Kind == kind && f.Id == id);
}

public sealed class BindingManifestSource : IBindingManifestSource
{
    private BindingManifestSource(IReadOnlyList<IManifestFragment> fragments) =>
        Fragments = fragments;

    public IReadOnlyList<IManifestFragment> Fragments { get; }

    public static BindingManifestSource Create(params IManifestFragment[] fragments) =>
        new(fragments);

    public static BindingManifestSource Create(IEnumerable<IManifestFragment> fragments) =>
        new(fragments.ToList());
}

public static class ManifestFragmentExtensions
{
    public static string Sha256Hex(this IManifestFragment fragment) =>
        ManifestFingerprint.Sha256Hex(fragment);
}
