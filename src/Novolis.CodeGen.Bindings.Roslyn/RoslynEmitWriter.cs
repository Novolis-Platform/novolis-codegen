using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Roslyn syntax transform hook invoked during binding emit.</summary>
/// <typeparam name="TPhase">Emitter phase enum.</typeparam>
/// <typeparam name="TContext">Emit context type (typically <see cref="Bindings.BindingEmitContext"/>).</typeparam>
public interface ICodegenHook<TPhase, in TContext>
    where TPhase : struct, Enum
{
    /// <summary>Execution order within the same <see cref="Phase"/> (lower runs first).</summary>
    int Order { get; }

    /// <summary>Phase this hook applies to.</summary>
    TPhase Phase { get; }

    /// <summary>Transforms the compilation unit for the current phase.</summary>
    /// <param name="unit">Parsed generated source.</param>
    /// <param name="context">Emit context.</param>
    /// <returns>Transformed compilation unit.</returns>
    CompilationUnitSyntax Transform(CompilationUnitSyntax unit, TContext context);
}

/// <summary>How emitted Roslyn syntax is formatted before writing to disk.</summary>
public enum FormatPolicy
{
    /// <summary>Use the Roslyn workspace formatter.</summary>
    RoslynFormatter,

    /// <summary>Normalize whitespace only (faster, less opinionated).</summary>
    NormalizeWhitespace,
}

/// <summary>Parses generated C# source into Roslyn syntax trees.</summary>
public static class CodegenSyntaxParser
{
    /// <summary>Parses generated source text into a <see cref="CompilationUnitSyntax"/>.</summary>
    /// <param name="source">Generated C# source.</param>
    /// <returns>Root compilation unit.</returns>
    public static CompilationUnitSyntax ParseGenerated(string source) =>
        (CompilationUnitSyntax)Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source, path: "").GetRoot();
}

/// <summary>Applies Roslyn hooks and formatting when writing generated binding files.</summary>
/// <typeparam name="TPhase">Emitter phase enum.</typeparam>
/// <typeparam name="TContext">Emit context type.</typeparam>
public static class RoslynEmitWriter<TPhase, TContext>
    where TPhase : struct, Enum
    where TContext : Bindings.BindingEmitContext
{
    /// <summary>Parses, transforms, formats, and writes generated source to <see cref="Bindings.BindingEmitContext.OutputPath"/>.</summary>
    /// <param name="rawSource">Unformatted generated C# source.</param>
    /// <param name="context">Emit context (paths and environment).</param>
    /// <param name="phase">Current emit phase for hook selection.</param>
    /// <param name="hooks">Registered codegen hooks.</param>
    /// <param name="formatPolicy">Formatting policy.</param>
    public static void WriteFile(
        string rawSource,
        TContext context,
        TPhase phase,
        IReadOnlyList<ICodegenHook<TPhase, TContext>> hooks,
        FormatPolicy formatPolicy)
    {
        var unit = CodegenSyntaxParser.ParseGenerated(rawSource);
        foreach (var hook in hooks.Where(h => EqualityComparer<TPhase>.Default.Equals(h.Phase, phase)).OrderBy(h => h.Order))
            unit = hook.Transform(unit, context);

        var formatted = formatPolicy == FormatPolicy.NormalizeWhitespace
            ? unit.NormalizeWhitespace(eol: Environment.NewLine).ToFullString()
            : CodegenFormatter.FormatCompilationUnit(unit);

        var outputDirectory = context.Environment.FileSystem.Path.GetDirectoryName(context.OutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            context.Environment.FileSystem.Directory.CreateDirectory(outputDirectory);
        if (!formatted.EndsWith('\n'))
            formatted += Environment.NewLine;

        context.Environment.FileSystem.File.WriteAllText(
            context.OutputPath,
            formatted,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

internal static class CodegenFormatter
{
    public static string FormatCompilationUnit(CompilationUnitSyntax unit)
    {
        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var formatted = Microsoft.CodeAnalysis.Formatting.Formatter.Format(unit, workspace);
        return formatted.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();
    }
}
