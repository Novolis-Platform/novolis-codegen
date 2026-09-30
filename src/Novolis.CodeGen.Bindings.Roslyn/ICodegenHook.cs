using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Roslyn syntax transform hook invoked during binding emit.</summary>
/// <typeparam name="TPhase">Emitter phase enum defined by the consumer.</typeparam>
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
