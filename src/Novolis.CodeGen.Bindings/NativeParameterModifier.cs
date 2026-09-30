namespace Novolis.CodeGen.Bindings;

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
