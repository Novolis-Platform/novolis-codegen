namespace Novolis.CodeGen.Bindings;

/// <summary>Controls the source formatting used after an emitter returns raw C#.</summary>
public enum BindingFormatPolicy
{
    /// <summary>Use the Roslyn workspace formatter.</summary>
    RoslynFormatter,

    /// <summary>Normalize whitespace without applying Roslyn workspace formatting.</summary>
    NormalizeWhitespace,
}
