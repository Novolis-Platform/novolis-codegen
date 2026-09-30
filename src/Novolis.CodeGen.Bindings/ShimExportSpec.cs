namespace Novolis.CodeGen.Bindings;

/// <summary>One export entry in a dynamic shim manifest.</summary>
/// <param name="Export">Native export symbol name.</param>
/// <param name="Signature">Typed C ABI function signature.</param>
public sealed record ShimExportSpec(string Export, NativeSignature Signature);
