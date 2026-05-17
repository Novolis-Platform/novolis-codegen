using Novolis.CodeGen.Reflection.ClassDiagram;
using TUnit.Core;

namespace Novolis.CodeGen.Reflection.ClassDiagram.Tests;

public class ClassDiagramBuilderTests
{
    [Test]
    public async Task Build_IncludesClassDiagramHeader()
    {
        var builder = new ClassDiagramBuilder(typeof(ClassDiagramBuilderTests).Assembly);
        var diagram = builder.Build();

        await Assert.That(diagram).StartsWith("classDiagram");
        await Assert.That(diagram).Contains("class ClassDiagramBuilderTests");
    }
}
