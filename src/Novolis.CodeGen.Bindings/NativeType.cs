namespace Novolis.CodeGen.Bindings;

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
