namespace Novolis.CodeGen.Bindings;

/// <summary>Fingerprint helpers for manifest fragments.</summary>
public static class ManifestFragmentExtensions
{
    /// <summary>Computes the SHA-256 hex fingerprint for <paramref name="fragment"/>.</summary>
    /// <param name="fragment">Manifest fragment.</param>
    /// <returns>64-character lowercase hex digest.</returns>
    public static string Sha256Hex(this IManifestFragment fragment) =>
        ManifestFingerprint.Sha256Hex(fragment);
}
