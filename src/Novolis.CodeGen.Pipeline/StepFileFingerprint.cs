using System.Security.Cryptography;

namespace Novolis.CodeGen.Pipeline;

public static class StepFileFingerprint
{
    public static string Sha256Hex(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    public static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

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
