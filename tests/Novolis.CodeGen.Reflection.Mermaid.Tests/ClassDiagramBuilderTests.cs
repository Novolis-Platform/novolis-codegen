using FluentAssertions;
using Novolis.CodeGen.Reflection.Mermaid;

namespace Novolis.CodeGen.Reflection.Mermaid.Tests;

public class ClassDiagramBuilderTests
{
    [Test]
    public void Build_IncludesClassDiagramHeader()
    {
        var builder = new ClassDiagramBuilder(typeof(ClassDiagramBuilderTests).Assembly);
        var diagram = builder.Build();

        diagram.Should().StartWith("classDiagram");
        diagram.Should().Contain("class ClassDiagramBuilderTests");
    }
}
