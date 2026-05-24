namespace Novolis.CodeGen.Reflection.ClassDiagram;

/// <summary>Builds a diagram representation (for example Mermaid) from reflected types.</summary>
public interface IDiagramBuilder
{
    /// <summary>Builds the diagram text.</summary>
    /// <returns>Diagram source (for example a Mermaid <c>classDiagram</c> block).</returns>
    string Build();
}
