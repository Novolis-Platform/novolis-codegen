using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

public interface ICodegenHook<TPhase, in TContext>
    where TPhase : struct, Enum
{
    int Order { get; }

    TPhase Phase { get; }

    CompilationUnitSyntax Transform(CompilationUnitSyntax unit, TContext context);
}

public enum FormatPolicy
{
    RoslynFormatter,
    NormalizeWhitespace,
}

public static class CodegenSyntaxParser
{
    public static CompilationUnitSyntax ParseGenerated(string source) =>
        (CompilationUnitSyntax)Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source, path: "").GetRoot();
}

public static class RoslynEmitWriter<TPhase, TContext>
    where TPhase : struct, Enum
    where TContext : Bindings.BindingEmitContext
{
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

        Directory.CreateDirectory(Path.GetDirectoryName(context.OutputPath)!);
        if (!formatted.EndsWith('\n'))
            formatted += Environment.NewLine;

        File.WriteAllText(context.OutputPath, formatted, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
