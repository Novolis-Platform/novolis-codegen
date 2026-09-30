namespace Novolis.CodeGen.Bindings;

/// <summary>One method on a generated façade type.</summary>
/// <param name="Name">Method name.</param>
/// <param name="Signature">Method signature (parameters and return type).</param>
/// <param name="Body">Method body source.</param>
/// <param name="Summary">Optional XML doc summary.</param>
public sealed record FacadeMethodSpec(
    string Name,
    string Signature,
    string Body,
    string? Summary = null);
