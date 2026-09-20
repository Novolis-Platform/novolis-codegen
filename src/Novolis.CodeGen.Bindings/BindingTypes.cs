namespace Novolis.CodeGen.Bindings;

/// <summary>Identifies the kind of binding manifest fragment.</summary>
public enum FragmentKind
{
    /// <summary>Native interop export definitions (LibraryImport / P/Invoke).</summary>
    InteropExports,

    /// <summary>Dynamic shim exports loaded from a native module.</summary>
    ShimExports,

    /// <summary>Debug capture hooks and symbol names.</summary>
    DebugConfig,

    /// <summary>Hand-authored façade types generated from manifest methods.</summary>
    FacadeTypes,

    /// <summary>Native artifact metadata (binaries, versions).</summary>
    NativeArtifacts,

    /// <summary>Source version pins for reproducible codegen.</summary>
    SourceVersions,
}

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

/// <summary>Controls the source formatting used after an emitter returns raw C#.</summary>
public enum BindingFormatPolicy
{
    /// <summary>Use the Roslyn workspace formatter.</summary>
    RoslynFormatter,

    /// <summary>Normalize whitespace without applying Roslyn workspace formatting.</summary>
    NormalizeWhitespace,
}

/// <summary>Describes a struct type emitted inside a dynamic-export binding class.</summary>
/// <param name="Name">CLR type name.</param>
/// <param name="Fields">Ordered field specifications.</param>
public sealed record EmbeddedTypeSpec(string Name, IReadOnlyList<EmbeddedFieldSpec> Fields);

/// <summary>Describes one field on an embedded struct type.</summary>
/// <param name="Name">Field name.</param>
/// <param name="ClrType">CLR type name as emitted in source.</param>
public sealed record EmbeddedFieldSpec(string Name, string ClrType);
