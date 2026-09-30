namespace Novolis.CodeGen.Bindings;

/// <summary>SHA-256 helpers for raw manifest bytes.</summary>
public static class ManifestHashing
{
    /// <summary>Returns lowercase hex SHA-256 of <paramref name="bytes"/>.</summary>
    /// <param name="bytes">Content to hash.</param>
    /// <returns>64-character hex digest.</returns>
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
}
