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
