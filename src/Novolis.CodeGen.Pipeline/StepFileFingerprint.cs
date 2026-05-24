using System.Security.Cryptography;

namespace Novolis.CodeGen.Pipeline;

/// <summary>SHA-256 fingerprinting for pipeline inputs and outputs.</summary>
public static class StepFileFingerprint
{
    /// <summary>Computes lowercase hex SHA-256 of a file.</summary>
    /// <param name="path">Absolute file path.</param>
    /// <returns>64-character hex digest.</returns>
    public static string Sha256Hex(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    /// <summary>Computes lowercase hex SHA-256 of a byte span.</summary>
    /// <param name="bytes">Content to hash.</param>
    /// <returns>64-character hex digest.</returns>
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>Builds a relative-path to SHA-256 map for existing input files.</summary>
    /// <param name="paths">Input paths (absolute or relative to <paramref name="repoRoot"/>).</param>
    /// <param name="repoRoot">Repository root for relative paths.</param>
    /// <returns>Map of repo-relative paths to digests.</returns>
    public static Dictionary<string, string> HashFiles(IEnumerable<string> paths, string repoRoot)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path);
            if (!File.Exists(full))
                continue;

            var rel = Path.GetRelativePath(repoRoot, full).Replace('\\', '/');
            map[rel] = Sha256Hex(full);
        }

        return map;
    }

    /// <summary>Describes output files with path, digest, and size for <c>result.json</c>.</summary>
    /// <param name="paths">Output paths.</param>
    /// <param name="repoRoot">Repository root.</param>
    /// <param name="stepDirForRelative">When set, paths under this directory are recorded relative to the step folder.</param>
    /// <returns>Output records for existing files.</returns>
    public static List<StepOutputRecord> DescribeOutputs(
        IEnumerable<string> paths,
        string repoRoot,
        string? stepDirForRelative = null)
    {
        var list = new List<StepOutputRecord>();
        foreach (var path in paths)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path);
            if (!File.Exists(full))
                continue;

            var rel = stepDirForRelative is not null &&
                      full.StartsWith(stepDirForRelative, StringComparison.OrdinalIgnoreCase)
                ? Path.GetRelativePath(stepDirForRelative, full).Replace('\\', '/')
                : Path.GetRelativePath(repoRoot, full).Replace('\\', '/');

            list.Add(new StepOutputRecord
            {
                Path = rel,
                Sha256 = Sha256Hex(full),
                Bytes = new FileInfo(full).Length,
            });
        }

        return list;
    }
}
