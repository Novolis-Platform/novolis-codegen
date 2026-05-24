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

/// <summary>Strongly typed identifier for an interop or shim code template.</summary>
/// <param name="Value">Template key used by emitters (for example <c>void_void</c>).</param>
public readonly record struct TemplateId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicit conversion from a string template key.</summary>
    /// <param name="value">Template key.</param>
    public static implicit operator TemplateId(string value) => new(value);

    /// <summary>Implicit conversion to the underlying template key string.</summary>
    /// <param name="id">Template identifier.</param>
    public static implicit operator string(TemplateId id) => id.Value;
}

/// <summary>Well-known interop templates for core raylib-style signatures.</summary>
public static class InteropTemplate
{
    /// <summary>Template for <c>void Name(void)</c> imports.</summary>
    public static readonly TemplateId VoidVoid = "void_void";

    /// <summary>Template for <c>void Name(Color)</c> imports.</summary>
    public static readonly TemplateId VoidColor = "void_color";

    /// <summary>Template for <c>void Name(int)</c> imports.</summary>
    public static readonly TemplateId VoidInt = "void_int";

    /// <summary>Template for <c>void Name(Camera3D)</c> imports.</summary>
    public static readonly TemplateId VoidCamera3d = "void_camera3d";
}

/// <summary>Well-known Dear ImGui interop templates.</summary>
public static class ImGuiTemplate
{
    /// <summary>Template for <c>void Name(void)</c> imports.</summary>
    public static readonly TemplateId VoidVoid = "void_void";

    /// <summary>Template for <c>void Name(int)</c> imports.</summary>
    public static readonly TemplateId VoidInt = "void_int";

    /// <summary>Template for <c>int Name(char*, int, int)</c> imports.</summary>
    public static readonly TemplateId IntUtf8PtrIntInt = "int_utf8_ptrint_int";
}

/// <summary>Well-known raygui interop templates.</summary>
public static class RayguiTemplate
{
    /// <summary>Template for rectangle + UTF-8 string parameters.</summary>
    public static readonly TemplateId IntRectUtf8 = "int_rect_utf8";

    /// <summary>Template for <c>void Name(void)</c> imports.</summary>
    public static readonly TemplateId VoidVoid = "void_void";
}

/// <summary>Describes a struct type embedded in generated raygui bindings.</summary>
/// <param name="Name">CLR type name.</param>
/// <param name="Fields">Ordered field specifications.</param>
public sealed record EmbeddedTypeSpec(string Name, IReadOnlyList<EmbeddedFieldSpec> Fields);

/// <summary>Describes one field on an embedded struct type.</summary>
/// <param name="Name">Field name.</param>
/// <param name="ClrType">CLR type name as emitted in source.</param>
public sealed record EmbeddedFieldSpec(string Name, string ClrType);

/// <summary>Embedded struct definitions used by raygui codegen.</summary>
public static class RayguiEmbeddedTypes
{
    /// <summary>Rectangle struct with X, Y, Width, and Height fields.</summary>
    public static readonly EmbeddedTypeSpec RayguiRectangle = new(
        "RayguiRectangle",
        [
            new EmbeddedFieldSpec("X", "float"),
            new EmbeddedFieldSpec("Y", "float"),
            new EmbeddedFieldSpec("Width", "float"),
            new EmbeddedFieldSpec("Height", "float"),
        ]);
}
