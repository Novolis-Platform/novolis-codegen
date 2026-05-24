using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

public sealed class CodegenEnvironment
{
    public required IFileSystem FileSystem { get; init; }

    public required string RepoRoot { get; init; }

    public static CodegenEnvironment Physical(string repoRoot) =>
        new() { FileSystem = new FileSystem(), RepoRoot = repoRoot };

    public string Combine(params ReadOnlySpan<string> relativeParts)
    {
        var path = RepoRoot;
        foreach (var part in relativeParts)
            path = FileSystem.Path.Combine(path, part);
        return path;
    }

    public bool FileExists(string relativePath) =>
        FileSystem.File.Exists(Combine(relativePath));

    public string ReadAllText(string relativePath) =>
        FileSystem.File.ReadAllText(Combine(relativePath));

    public byte[] ReadAllBytes(string relativePath) =>
        FileSystem.File.ReadAllBytes(Combine(relativePath));

    public void WriteAllText(string relativePath, string contents)
    {
        var full = Combine(relativePath);
        var dir = FileSystem.Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir))
            FileSystem.Directory.CreateDirectory(dir);
        FileSystem.File.WriteAllText(full, contents, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void WriteAllTextAbsolute(string absolutePath, string contents)
    {
        var dir = FileSystem.Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(dir))
            FileSystem.Directory.CreateDirectory(dir);
        FileSystem.File.WriteAllText(absolutePath, contents, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
