namespace Novolis.CodeGen.Bindings;

/// <summary>Describes a struct type emitted inside a dynamic-export binding class.</summary>
/// <param name="Name">CLR type name.</param>
/// <param name="Fields">Ordered field specifications.</param>
public sealed record EmbeddedTypeSpec(string Name, IReadOnlyList<EmbeddedFieldSpec> Fields);
