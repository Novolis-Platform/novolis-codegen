using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Xsd;

/// <summary>Writes <see cref="CompilationUnitSyntax"/> trees as formatted C# text.</summary>
public static class SyntaxEmitWriter
{
    /// <summary>Formats a compilation unit to source text.</summary>
    public static string Format(CompilationUnitSyntax unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var wantsNullable = unit.GetLeadingTrivia()
            .Any(t => t.IsKind(SyntaxKind.NullableDirectiveTrivia));
        using var workspace = new AdhocWorkspace();
        var formatted = Formatter.Format(unit.NormalizeWhitespace(eol: "\n"), workspace);
        var text = formatted.ToFullString();
        if (wantsNullable && !text.StartsWith("#nullable", StringComparison.Ordinal))
            text = "#nullable enable\n" + text;
        return text;
    }

    /// <summary>Writes all files from an <see cref="EmitResult"/> under <paramref name="outputDirectory"/>.</summary>
    public static IReadOnlyList<string> WriteAll(EmitResult result, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var written = new List<string>();
        foreach (var file in result.Files)
        {
            var path = Path.Combine(outputDirectory, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var text = Format(file.CompilationUnit);
            if (!text.EndsWith('\n'))
                text += "\n";
            File.WriteAllText(path, text);
            written.Add(path);
        }

        return written;
    }
}
