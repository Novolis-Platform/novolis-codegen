namespace Novolis.CodeGen.Bindings;

public enum FragmentKind
{
    InteropExports,
    ShimExports,
    DebugConfig,
    FacadeTypes,
    NativeArtifacts,
    SourceVersions,
}

public enum EmitStrategy
{
    LibraryImport,
    DynamicExports,
    DebugHooks,
    FacadeForward,
}

public readonly record struct TemplateId(string Value)
{
    public override string ToString() => Value;

    public static implicit operator TemplateId(string value) => new(value);

    public static implicit operator string(TemplateId id) => id.Value;
}

public static class InteropTemplate
{
    public static readonly TemplateId VoidVoid = "void_void";
    public static readonly TemplateId VoidColor = "void_color";
    public static readonly TemplateId VoidInt = "void_int";
    public static readonly TemplateId VoidCamera3d = "void_camera3d";
}

public static class ImGuiTemplate
{
    public static readonly TemplateId VoidVoid = "void_void";
    public static readonly TemplateId VoidInt = "void_int";
    public static readonly TemplateId IntUtf8PtrIntInt = "int_utf8_ptrint_int";
}

public static class RayguiTemplate
{
    public static readonly TemplateId IntRectUtf8 = "int_rect_utf8";
    public static readonly TemplateId VoidVoid = "void_void";
}

public sealed record EmbeddedTypeSpec(string Name, IReadOnlyList<EmbeddedFieldSpec> Fields);

public sealed record EmbeddedFieldSpec(string Name, string ClrType);

public static class RayguiEmbeddedTypes
{
    public static readonly EmbeddedTypeSpec RayguiRectangle = new(
        "RayguiRectangle",
        [
            new EmbeddedFieldSpec("X", "float"),
            new EmbeddedFieldSpec("Y", "float"),
            new EmbeddedFieldSpec("Width", "float"),
            new EmbeddedFieldSpec("Height", "float"),
        ]);
}
