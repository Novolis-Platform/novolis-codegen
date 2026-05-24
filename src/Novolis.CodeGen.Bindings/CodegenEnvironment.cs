using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>
/// Filesystem-backed view of a repository root used by binding codegen and pipeline steps.
/// </summary>
public sealed class CodegenEnvironment
{
    /// <summary>Abstraction used for all file I/O (enables testing with an in-memory filesystem).</summary>
    public required IFileSystem FileSystem { get; init; }

    /// <summary>Absolute path to the repository root.</summary>
    public required string RepoRoot { get; init; }

    /// <summary>Creates an environment that reads and writes the physical filesystem under <paramref name="repoRoot"/>.</summary>
    /// <param name="repoRoot">Repository root directory.</param>
    /// <returns>A configured <see cref="CodegenEnvironment"/>.</returns>
    public static CodegenEnvironment Physical(string repoRoot) =>
        new() { FileSystem = new FileSystem(), RepoRoot = repoRoot };

    /// <summary>Combines <see cref="RepoRoot"/> with one or more relative path segments.</summary>
    /// <param name="relativeParts">Path segments relative to the repository root.</param>
    /// <returns>An absolute path.</returns>
    public string Combine(params ReadOnlySpan<string> relativeParts)
    {
        var path = RepoRoot;
        foreach (var part in relativeParts)
            path = FileSystem.Path.Combine(path, part);
        return path;
    }

    /// <summary>Returns whether a file exists relative to <see cref="RepoRoot"/>.</summary>
    /// <param name="relativePath">Path relative to the repository root.</param>
    /// <returns><see langword="true"/> when the file exists.</returns>
    public bool FileExists(string relativePath) =>
        FileSystem.File.Exists(Combine(relativePath));

    /// <summary>Reads all text from a file relative to <see cref="RepoRoot"/>.</summary>
    /// <param name="relativePath">Path relative to the repository root.</param>
    /// <returns>File contents.</returns>
    public string ReadAllText(string relativePath) =>
        FileSystem.File.ReadAllText(Combine(relativePath));

    /// <summary>Reads all bytes from a file relative to <see cref="RepoRoot"/>.</summary>
    /// <param name="relativePath">Path relative to the repository root.</param>
    /// <returns>File contents.</returns>
    public byte[] ReadAllBytes(string relativePath) =>
        FileSystem.File.ReadAllBytes(Combine(relativePath));

    /// <summary>Writes UTF-8 text to a path relative to <see cref="RepoRoot"/>, creating parent directories as needed.</summary>
    /// <param name="relativePath">Path relative to the repository root.</param>
    /// <param name="contents">Text to write.</param>
    public void WriteAllText(string relativePath, string contents)
    {
        var full = Combine(relativePath);
        var dir = FileSystem.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            FileSystem.Directory.CreateDirectory(dir);
        FileSystem.File.WriteAllText(full, contents, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Writes UTF-8 text to an absolute path, creating parent directories as needed.</summary>
    /// <param name="absolutePath">Absolute output path.</param>
    /// <param name="contents">Text to write.</param>
    public void WriteAllTextAbsolute(string absolutePath, string contents)
    {
        var dir = FileSystem.Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
            FileSystem.Directory.CreateDirectory(dir);
        FileSystem.File.WriteAllText(absolutePath, contents, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
