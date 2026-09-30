using System.Text;

namespace Novolis.CodeGen.Bindings;

/// <summary>Provides consumer-specific documentation for generated façade types and methods.</summary>
public interface IFacadeDocumentationResolver
{
    /// <summary>Gets the type summary for a generated façade.</summary>
    /// <param name="type">The façade type being emitted.</param>
    /// <returns>A summary, or <see langword="null"/> when no documentation should be emitted.</returns>
    string? ResolveTypeSummary(FacadeTypeSpec type);

    /// <summary>Gets the method summary for a generated façade method.</summary>
    /// <param name="type">The enclosing façade type.</param>
    /// <param name="method">The façade method being emitted.</param>
    /// <returns>A summary, or <see langword="null"/> when no documentation should be emitted.</returns>
    string? ResolveMethodSummary(FacadeTypeSpec type, FacadeMethodSpec method);
}
