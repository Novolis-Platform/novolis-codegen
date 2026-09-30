using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Applies Roslyn hooks and formatting when writing generated binding files.</summary>
/// <typeparam name="TPhase">Emitter phase enum defined by the consumer.</typeparam>
/// <typeparam name="TContext">Emit context type (must inherit <see cref="Bindings.BindingEmitContext"/>).</typeparam>
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
            ? unit.NormalizeWhitespace(eol: "\n").ToFullString()
            : CodegenFormatter.FormatCompilationUnit(unit);

        formatted = formatted.Replace("\r\n", "\n");
        if (!formatted.EndsWith('\n'))
            formatted += "\n";

        context.Environment.WriteAllTextAbsolute(context.OutputPath, formatted);
    }
}
