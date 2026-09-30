namespace Novolis.CodeGen.Bindings;

/// <summary>One named parameter in a C ABI signature.</summary>
/// <param name="Name">The generated C# parameter name.</param>
/// <param name="Type">The native ABI type.</param>
/// <param name="Modifier">The generated C# parameter modifier.</param>
public sealed record NativeParameter(
    string Name,
    NativeType Type,
    NativeParameterModifier Modifier = NativeParameterModifier.None);
