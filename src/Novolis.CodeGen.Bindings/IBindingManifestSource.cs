namespace Novolis.CodeGen.Bindings;

/// <summary>Provides binding manifest fragments consumed by codegen emitters.</summary>
public interface IBindingManifestSource
{
    /// <summary>All manifest fragments available to the host.</summary>
    IReadOnlyList<IManifestFragment> Fragments { get; }
}
