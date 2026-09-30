namespace Novolis.CodeGen.Bindings;

/// <summary>One hand-authored façade type emitted from manifest data.</summary>
/// <param name="Name">Type name.</param>
/// <param name="Namespace">CLR namespace.</param>
/// <param name="Folder">Relative folder under the project.</param>
/// <param name="TypeSummary">Optional type-level XML summary.</param>
/// <param name="Usings">Additional using directives.</param>
/// <param name="Methods">Methods to emit.</param>
public sealed record FacadeTypeSpec(
    string Name,
    string Namespace,
    string Folder,
    string? TypeSummary,
    IReadOnlyList<string> Usings,
    IReadOnlyList<FacadeMethodSpec> Methods);
