using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Emits binding source from a manifest fragment for a specific <see cref="EmitStrategy"/>.</summary>
public interface IBindingEmitter
{
    /// <summary>Strategy implemented by this emitter.</summary>
    EmitStrategy Strategy { get; }

    /// <summary>Generates source text for <paramref name="request"/>.</summary>
    /// <param name="request">Emit request.</param>
    /// <returns>Generated C# source (not yet formatted).</returns>
    string Emit(EmitRequest request);
}
