using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Describes one generated output file (class name, strategy, paths).</summary>
/// <param name="ClassName">Generated type name.</param>
/// <param name="Strategy">Emitter strategy.</param>
/// <param name="RelativePath">Output path relative to the repository root.</param>
/// <param name="Namespace">CLR namespace for the generated type.</param>
/// <param name="AssemblyName">Target assembly name.</param>
/// <param name="LibraryConstantName">Optional generated constant naming the native library.</param>
/// <param name="TypeSummary">Optional generated type XML summary.</param>
/// <param name="StructSummary">Optional generated struct XML summary.</param>
/// <param name="FacadeMethodImpl">Optional method implementation policy for façade forwards.</param>
public sealed record EmitTarget(
    string ClassName,
    EmitStrategy Strategy,
    string RelativePath,
    string Namespace,
    string AssemblyName,
    string? LibraryConstantName = null,
    string? TypeSummary = null,
    string? StructSummary = null,
    string? FacadeMethodImpl = null);
