namespace Novolis.CodeGen.Bindings;

/// <summary>Interop marshalling policy applied when emitting LibraryImport stubs.</summary>
/// <param name="SuppressGcTransitionByFunction">Function names for which GC transition suppression is enabled.</param>
/// <param name="NeverSuppressGcTransition">Import names that must never suppress GC transition.</param>
/// <param name="FacadeMethodImpl">Optional MethodImpl attribute text for façade methods.</param>
/// <param name="UseDisableRuntimeMarshalling">When <see langword="true"/>, emit DisableRuntimeMarshalling at assembly level.</param>
public sealed record InteropPolicySpec(
    IReadOnlyList<string> SuppressGcTransitionByFunction,
    IReadOnlyList<string> NeverSuppressGcTransition,
    string? FacadeMethodImpl,
    bool UseDisableRuntimeMarshalling);
