using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class BindingTypesTests
{
    [Test]
    public async Task Template_constants_and_embedded_types_are_defined()
    {
        await Assert.That(ImGuiTemplate.VoidVoid.Value).IsEqualTo("void_void");
        await Assert.That(ImGuiTemplate.VoidInt.Value).IsEqualTo("void_int");
        await Assert.That(ImGuiTemplate.IntUtf8PtrIntInt.Value).IsEqualTo("int_utf8_ptrint_int");
        await Assert.That(RayguiTemplate.IntRectUtf8.Value).IsEqualTo("int_rect_utf8");
        await Assert.That(RayguiTemplate.VoidVoid.Value).IsEqualTo("void_void");
        await Assert.That(RayguiEmbeddedTypes.RayguiRectangle.Name).IsEqualTo("RayguiRectangle");
        await Assert.That(RayguiEmbeddedTypes.RayguiRectangle.Fields.Count()).IsEqualTo(4);
        await Assert.That(RayguiEmbeddedTypes.RayguiRectangle.Fields[0].Name).IsEqualTo("X");
        await Assert.That(RayguiEmbeddedTypes.RayguiRectangle.Fields[0].ClrType).IsEqualTo("float");

        var field = new EmbeddedFieldSpec("Width", "float");
        var type = new EmbeddedTypeSpec("Rect", [field]);
        await Assert.That(type.Fields[0].Name).IsEqualTo("Width");
        await Assert.That(InteropTemplate.VoidVoid.ToString()).IsEqualTo("void_void");
        await Assert.That((string)InteropTemplate.VoidColor).IsEqualTo("void_color");
    }
}
