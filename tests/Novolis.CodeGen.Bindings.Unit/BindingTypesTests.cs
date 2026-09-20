using Novolis.CodeGen.Bindings;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class BindingTypesTests
{
    [Test]
    public async Task Native_signatures_and_embedded_types_are_typed()
    {
        var signature = NativeSignature.Create(
            NativeType.Int32,
            new NativeParameter("text", NativeType.Utf8String),
            new NativeParameter("value", NativeType.Float, NativeParameterModifier.Out));

        var field = new EmbeddedFieldSpec("Width", "float");
        var type = new EmbeddedTypeSpec("Rect", [field]);

        await Assert.That(signature.ReturnType.CSharpTypeName).IsEqualTo("int");
        await Assert.That(signature.Parameters[0].Type.CSharpTypeName).IsEqualTo("string");
        await Assert.That(signature.Parameters[1].Modifier).IsEqualTo(NativeParameterModifier.Out);
        await Assert.That(type.Fields[0].Name).IsEqualTo("Width");
        await Assert.That(NativeType.Named("RayguiRectangle").CSharpTypeName).IsEqualTo("RayguiRectangle");
        await Assert.That(NativeType.BytePointer.CSharpTypeName).IsEqualTo("byte*");
    }
}
