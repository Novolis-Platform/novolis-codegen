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
