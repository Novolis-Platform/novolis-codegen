using System.IO.Abstractions;

namespace Novolis.CodeGen.Reflection.Dump;

/// <summary>
/// Persists VarDump-style C# source for objects under a root directory (<c>{id}.cs</c>).
/// Useful for embedding trained models, fixtures, and snapshots as compilable source.
/// </summary>
public sealed class DumpFileStore
{
    private readonly string _rootDirectory;
    private readonly IFileSystem _fileSystem;

    /// <summary>Creates a store using the physical file system.</summary>
    /// <param name="rootDirectory">Directory that will hold <c>.cs</c> dump files.</param>
    public DumpFileStore(string rootDirectory)
        : this(rootDirectory, new FileSystem())
    {
    }

    /// <summary>Creates a store with an injectable file system (for tests).</summary>
    /// <param name="rootDirectory">Directory that will hold <c>.cs</c> dump files.</param>
    /// <param name="fileSystem">File system abstraction.</param>
    public DumpFileStore(string rootDirectory, IFileSystem fileSystem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentNullException.ThrowIfNull(fileSystem);
        _rootDirectory = rootDirectory;
        _fileSystem = fileSystem;
    }

    /// <summary>Absolute root directory for dump files.</summary>
    public string RootDirectory => _rootDirectory;

    /// <summary>Resolves the <c>.cs</c> path for an id.</summary>
    /// <param name="id">Dump identity (file name without extension).</param>
    /// <returns>Full path to <c>{id}.cs</c>.</returns>
    public string GetPath(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var safe = DumpHelper.ToFileSafeId(id);
        return _fileSystem.Path.Combine(_rootDirectory, $"{safe}.cs");
    }

    /// <summary>Writes <paramref name="obj"/> as a dumped class declaration.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="id">Dump identity (file name without extension).</param>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path written.</returns>
    public async ValueTask<string> SaveClassAsync<T>(
        string id,
        T obj,
        VarDump.Visitor.DumpOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        var source = obj.DumpClass(options);
        await WriteAsync(path, source, cancellationToken).ConfigureAwait(false);
        return path;
    }

    /// <summary>Writes <paramref name="obj"/> as a dumped variable initialization.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="id">Dump identity (file name without extension).</param>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path written.</returns>
    public async ValueTask<string> SaveVarAsync<T>(
        string id,
        T obj,
        VarDump.Visitor.DumpOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        var source = obj.DumpVar(options);
        await WriteAsync(path, source, cancellationToken).ConfigureAwait(false);
        return path;
    }

    /// <summary>Writes <paramref name="obj"/> as a dumped factory method.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="id">Dump identity (file name without extension).</param>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path written.</returns>
    public async ValueTask<string> SaveMethodAsync<T>(
        string id,
        T obj,
        VarDump.Visitor.DumpOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        var source = obj.DumpMethod(options);
        await WriteAsync(path, source, cancellationToken).ConfigureAwait(false);
        return path;
    }

    /// <summary>Loads raw dump source text, or null when missing.</summary>
    /// <param name="id">Dump identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>C# source text, or null.</returns>
    public async ValueTask<string?> LoadSourceAsync(string id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (!_fileSystem.File.Exists(path))
            return null;
        return await _fileSystem.File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lists dump ids (file names without <c>.cs</c>), newest write time first when available.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered dump identities.</returns>
    public ValueTask<IReadOnlyList<string>> ListIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Directory.CreateDirectory(_rootDirectory);

        var entries = new List<(string Id, DateTime Utc)>();
        foreach (var file in _fileSystem.Directory.EnumerateFiles(_rootDirectory, "*.cs"))
        {
            var name = _fileSystem.Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var utc = _fileSystem.File.GetLastWriteTimeUtc(file);
            entries.Add((name, utc));
        }

        entries.Sort(static (a, b) => b.Utc.CompareTo(a.Utc));
        IReadOnlyList<string> ids = entries.ConvertAll(static e => e.Id);
        return ValueTask.FromResult(ids);
    }

    /// <summary>Deletes a dump file when present.</summary>
    /// <param name="id">Dump identity.</param>
    /// <returns><see langword="true"/> when a file was deleted.</returns>
    public bool Delete(string id)
    {
        var path = GetPath(id);
        if (!_fileSystem.File.Exists(path))
            return false;
        _fileSystem.File.Delete(path);
        return true;
    }

    private async ValueTask WriteAsync(string path, string source, CancellationToken cancellationToken)
    {
        _fileSystem.Directory.CreateDirectory(_rootDirectory);
        await _fileSystem.File.WriteAllTextAsync(path, source, cancellationToken).ConfigureAwait(false);
    }
}
