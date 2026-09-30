namespace Novolis.CodeGen.Bindings;

/// <summary>Typed slice of a binding manifest (interop, shim, debug, façade, etc.).</summary>
public interface IManifestFragment
{
    /// <summary>Stable fragment identifier within its <see cref="Kind"/>.</summary>
    string Id { get; }

    /// <summary>Fragment kind.</summary>
    FragmentKind Kind { get; }
}
