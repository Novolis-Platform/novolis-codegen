namespace Novolis.CodeGen.Bindings;

/// <summary>Struct layout declared in an interop manifest.</summary>
/// <param name="Name">Struct name.</param>
/// <param name="Fields">Ordered fields.</param>
public sealed record InteropStructSpec(string Name, IReadOnlyList<InteropFieldSpec> Fields);
