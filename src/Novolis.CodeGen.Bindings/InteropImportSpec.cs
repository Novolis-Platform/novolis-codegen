namespace Novolis.CodeGen.Bindings;

/// <summary>One native import entry in an interop manifest.</summary>
/// <param name="Name">Native function name.</param>
/// <param name="Signature">Typed C ABI signature.</param>
/// <param name="Description">Optional XML doc summary for the generated member.</param>
/// <param name="SuppressGcTransition">Per-import GC transition override.</param>
public sealed record InteropImportSpec(
    string Name,
    NativeSignature Signature,
    string? Description = null,
    bool? SuppressGcTransition = null);
