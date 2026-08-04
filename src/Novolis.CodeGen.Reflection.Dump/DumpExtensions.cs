using System.IO.Abstractions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Reflection.Dump;

/// <summary>Var-dump style helpers that emit C# initialization or declaration syntax from runtime objects.</summary>
public static class DumpExtensions
{
    /// <summary>Dumps <paramref name="obj"/> as a variable initialization expression.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>C# source text.</returns>
    public static string DumpVar<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => VariableFactory.DumpVar(obj, options);

    /// <summary>Dumps <paramref name="obj"/> as a class declaration with properties and fields.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>C# source text.</returns>
    public static string DumpClass<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => ClassFactory.CreateClass(obj, options);

    /// <summary>Dumps <paramref name="obj"/> as a method that returns a reconstructed instance.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>C# source text.</returns>
    public static string DumpMethod<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => MethodFactory.CreateMethod(obj, options);

    /// <summary>Dumps a sequence as a class with one property per item.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="objs">Objects to dump.</param>
    /// <param name="idSelector">Selects the property name for each element.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>C# source text.</returns>
    public static string DumpEnumerable<T>(this IEnumerable<T> objs, Func<T, string> idSelector, VarDump.Visitor.DumpOptions? options = null) => ClassFactory.CreateEnumerableClass(objs, idSelector, options);

    /// <summary>Dumps <paramref name="obj"/> as a Roslyn <see cref="ClassDeclarationSyntax"/>.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>Parsed and normalized class declaration syntax.</returns>
    public static ClassDeclarationSyntax DumpClassDeclarationSyntax<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
        => SyntaxFactory.ParseSyntaxTree(DumpClass(obj, options)).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First().NormalizeWhitespace();

    /// <summary>Dumps <paramref name="obj"/> as a Roslyn <see cref="MethodDeclarationSyntax"/>.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="options">Optional dump options.</param>
    /// <returns>Parsed and normalized method declaration syntax.</returns>
    /// <exception cref="Exception">When generated text cannot be parsed as a method.</exception>
    public static MethodDeclarationSyntax DumpMethodDeclarationSyntax<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
        => AsMethodDeclarationSyntax(SyntaxFactory.ParseMemberDeclaration(MethodFactory.CreateMethod(obj, options)) ?? throw new Exception("Could not parse method declaration syntax.")).NormalizeWhitespace();

    /// <summary>Writes a class dump of <paramref name="obj"/> to <paramref name="path"/>.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="path">Destination <c>.cs</c> path.</param>
    /// <param name="fileSystem">Optional file system (defaults to physical).</param>
    /// <param name="options">Optional dump options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path written.</returns>
    public static async ValueTask<string> DumpClassToFileAsync<T>(
        this T obj,
        string path,
        IFileSystem? fileSystem = null,
        VarDump.Visitor.DumpOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fs = fileSystem ?? new FileSystem();
        var directory = fs.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            fs.Directory.CreateDirectory(directory);
        await fs.File.WriteAllTextAsync(path, obj.DumpClass(options), cancellationToken).ConfigureAwait(false);
        return path;
    }

    /// <summary>Writes a variable dump of <paramref name="obj"/> to <paramref name="path"/>.</summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="obj">Object to dump.</param>
    /// <param name="path">Destination <c>.cs</c> path.</param>
    /// <param name="fileSystem">Optional file system (defaults to physical).</param>
    /// <param name="options">Optional dump options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path written.</returns>
    public static async ValueTask<string> DumpVarToFileAsync<T>(
        this T obj,
        string path,
        IFileSystem? fileSystem = null,
        VarDump.Visitor.DumpOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fs = fileSystem ?? new FileSystem();
        var directory = fs.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            fs.Directory.CreateDirectory(directory);
        await fs.File.WriteAllTextAsync(path, obj.DumpVar(options), cancellationToken).ConfigureAwait(false);
        return path;
    }

    private static MethodDeclarationSyntax AsMethodDeclarationSyntax(MemberDeclarationSyntax memberDeclarationSyntax)
        => memberDeclarationSyntax as MethodDeclarationSyntax ?? throw new Exception("Could not parse method declaration syntax.");
}
