namespace Novolis.CodeGen.Bindings;

/// <summary>One field on an interop struct.</summary>
/// <param name="Name">Field name.</param>
/// <param name="ClrType">CLR type name as emitted in source.</param>
public sealed record InteropFieldSpec(string Name, string ClrType);
