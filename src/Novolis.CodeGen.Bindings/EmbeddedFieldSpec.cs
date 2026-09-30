namespace Novolis.CodeGen.Bindings;

/// <summary>Describes one field on an embedded struct type.</summary>
/// <param name="Name">Field name.</param>
/// <param name="ClrType">CLR type name as emitted in source.</param>
public sealed record EmbeddedFieldSpec(string Name, string ClrType);
