namespace Novolis.CodeGen.Bindings;

/// <summary>Selects how a binding emitter writes output for a target.</summary>
public enum EmitStrategy
{
    /// <summary>Emit <c>LibraryImport</c> interop stubs.</summary>
    LibraryImport,

    /// <summary>Emit dynamic export tables for shims.</summary>
    DynamicExports,

    /// <summary>Emit debug hook wiring.</summary>
    DebugHooks,

    /// <summary>Emit thin façade types that forward to generated interop.</summary>
    FacadeForward,
}
