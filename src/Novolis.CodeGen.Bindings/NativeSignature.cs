namespace Novolis.CodeGen.Bindings;

/// <summary>Identifies a C ABI type that can appear in a generated binding signature.</summary>
public enum NativeTypeKind
{
    /// <summary>The C <c>void</c> type.</summary>
    Void,

    /// <summary>A C boolean represented as a managed <see cref="bool"/>.</summary>
    Boolean,

    /// <summary>An eight-bit signed integer.</summary>
    Int8,

    /// <summary>An eight-bit unsigned integer.</summary>
    UInt8,

    /// <summary>A sixteen-bit signed integer.</summary>
    Int16,

    /// <summary>A sixteen-bit unsigned integer.</summary>
    UInt16,

    /// <summary>A thirty-two-bit signed integer.</summary>
    Int32,

    /// <summary>A thirty-two-bit unsigned integer.</summary>
    UInt32,

    /// <summary>A sixty-four-bit signed integer.</summary>
    Int64,

    /// <summary>A sixty-four-bit unsigned integer.</summary>
    UInt64,

    /// <summary>A single-precision floating-point value.</summary>
    Float,

    /// <summary>A double-precision floating-point value.</summary>
    Double,

    /// <summary>A native-sized signed integer or opaque handle.</summary>
    NativeInt,

    /// <summary>A UTF-8 C string projected as a managed <see cref="string"/>.</summary>
    Utf8String,

    /// <summary>A pointer to an eight-bit unsigned integer.</summary>
    BytePointer,

    /// <summary>A consumer-defined blittable CLR type.</summary>
    Named,
}

/// <summary>Typed C ABI type metadata used by library-import and dynamic-export emitters.</summary>
/// <param name="Kind">The primitive or named type category.</param>
/// <param name="ClrTypeName">The CLR type name for <see cref="NativeTypeKind.Named"/>.</param>
public sealed record NativeType(NativeTypeKind Kind, string? ClrTypeName = null)
{
    /// <summary>Gets the C <c>void</c> type.</summary>
    public static NativeType Void { get; } = new(NativeTypeKind.Void);

    /// <summary>Gets the managed boolean type.</summary>
    public static NativeType Boolean { get; } = new(NativeTypeKind.Boolean);

    /// <summary>Gets the 8-bit signed integer type.</summary>
    public static NativeType Int8 { get; } = new(NativeTypeKind.Int8);

    /// <summary>Gets the 8-bit unsigned integer type.</summary>
    public static NativeType UInt8 { get; } = new(NativeTypeKind.UInt8);

    /// <summary>Gets the 16-bit signed integer type.</summary>
    public static NativeType Int16 { get; } = new(NativeTypeKind.Int16);

    /// <summary>Gets the 16-bit unsigned integer type.</summary>
    public static NativeType UInt16 { get; } = new(NativeTypeKind.UInt16);

    /// <summary>Gets the 32-bit signed integer type.</summary>
    public static NativeType Int32 { get; } = new(NativeTypeKind.Int32);

    /// <summary>Gets the 32-bit unsigned integer type.</summary>
    public static NativeType UInt32 { get; } = new(NativeTypeKind.UInt32);

    /// <summary>Gets the 64-bit signed integer type.</summary>
    public static NativeType Int64 { get; } = new(NativeTypeKind.Int64);

    /// <summary>Gets the 64-bit unsigned integer type.</summary>
    public static NativeType UInt64 { get; } = new(NativeTypeKind.UInt64);

    /// <summary>Gets the single-precision floating-point type.</summary>
    public static NativeType Float { get; } = new(NativeTypeKind.Float);

    /// <summary>Gets the double-precision floating-point type.</summary>
    public static NativeType Double { get; } = new(NativeTypeKind.Double);

    /// <summary>Gets the native-sized integer / opaque-handle type.</summary>
    public static NativeType NativeInt { get; } = new(NativeTypeKind.NativeInt);

    /// <summary>Gets the UTF-8 string type.</summary>
    public static NativeType Utf8String { get; } = new(NativeTypeKind.Utf8String);

    /// <summary>Gets the unmanaged byte-pointer type.</summary>
    public static NativeType BytePointer { get; } = new(NativeTypeKind.BytePointer);

    /// <summary>Creates a consumer-defined blittable type.</summary>
    /// <param name="clrTypeName">The CLR type emitted in generated source.</param>
    /// <returns>The named native type.</returns>
    public static NativeType Named(string clrTypeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clrTypeName);
        return new NativeType(NativeTypeKind.Named, clrTypeName);
    }

    /// <summary>Gets the C# type spelling used in generated source.</summary>
    /// <exception cref="InvalidOperationException">A named type has no CLR name.</exception>
    public string CSharpTypeName => Kind switch
    {
        NativeTypeKind.Void => "void",
        NativeTypeKind.Boolean => "bool",
        NativeTypeKind.Int8 => "sbyte",
        NativeTypeKind.UInt8 => "byte",
        NativeTypeKind.Int16 => "short",
        NativeTypeKind.UInt16 => "ushort",
        NativeTypeKind.Int32 => "int",
        NativeTypeKind.UInt32 => "uint",
        NativeTypeKind.Int64 => "long",
        NativeTypeKind.UInt64 => "ulong",
        NativeTypeKind.Float => "float",
        NativeTypeKind.Double => "double",
        NativeTypeKind.NativeInt => "nint",
        NativeTypeKind.Utf8String => "string",
        NativeTypeKind.BytePointer => "byte*",
        NativeTypeKind.Named when !string.IsNullOrWhiteSpace(ClrTypeName) => ClrTypeName,
        NativeTypeKind.Named => throw new InvalidOperationException("A named native type requires a CLR type name."),
        _ => throw new ArgumentOutOfRangeException(),
    };
}

/// <summary>Controls how a parameter is written in generated C#.</summary>
public enum NativeParameterModifier
{
    /// <summary>Pass the value directly.</summary>
    None,

    /// <summary>Pass the value with the C# <c>in</c> modifier.</summary>
    In,

    /// <summary>Pass the value with the C# <c>out</c> modifier.</summary>
    Out,
}

/// <summary>One named parameter in a C ABI signature.</summary>
/// <param name="Name">The generated C# parameter name.</param>
/// <param name="Type">The native ABI type.</param>
/// <param name="Modifier">The generated C# parameter modifier.</param>
public sealed record NativeParameter(
    string Name,
    NativeType Type,
    NativeParameterModifier Modifier = NativeParameterModifier.None);

/// <summary>One fully typed native C ABI function signature.</summary>
/// <param name="ReturnType">The function return type.</param>
/// <param name="Parameters">Ordered named function parameters.</param>
public sealed record NativeSignature(NativeType ReturnType, IReadOnlyList<NativeParameter> Parameters)
{
    /// <summary>Creates a signature with the supplied return type and parameters.</summary>
    /// <param name="returnType">The function return type.</param>
    /// <param name="parameters">Ordered named parameters.</param>
    /// <returns>A typed C ABI signature.</returns>
    public static NativeSignature Create(NativeType returnType, params NativeParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(returnType);
        ArgumentNullException.ThrowIfNull(parameters);
        return new NativeSignature(returnType, parameters);
    }
}
