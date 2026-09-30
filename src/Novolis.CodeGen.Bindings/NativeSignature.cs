namespace Novolis.CodeGen.Bindings;

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
