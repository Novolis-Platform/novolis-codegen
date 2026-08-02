using System.Reflection;
using Novolis.CodeGen.Reflection;

namespace Novolis.CodeGen.Reflection.Tests;

public sealed class ReflectionExtensionsExtendedTests
{
    [Test]
    public async Task TypeExtensions_handle_nullable_and_arrays()
    {
        await Assert.That(typeof(int?).GetDisplayName()).IsEqualTo("NullableNullableOfInteger");
        await Assert.That(typeof(int[]).GetFriendlyName()).IsEqualTo("Int32[]");
        await Assert.That(typeof(List<string>).GetDisplayName()).IsEqualTo("ListOfString");
    }

    [Test]
    public async Task ParameterInfoExtensions_format_names()
    {
        var method = typeof(SampleMethods).GetMethod(nameof(SampleMethods.Add))!;
        var param = method.GetParameters()[0];
        await Assert.That(param.GetDisplayName()).IsEqualTo("a : Integer");
        await Assert.That(param.GetFullDisplayName()).Contains("System.Integer");
    }

    [Test]
    public async Task ConstructorInfoExtensions_format_names()
    {
        var ctor = typeof(SampleMethods).GetConstructor([typeof(int)])!;
        await Assert.That(ctor.GetDisplayName()).Contains("SampleMethods");
        await Assert.That(ctor.GetFullDisplayName()).Contains("SampleMethods");
    }

    [Test]
    public async Task StringExtensions_first_and_last_token()
    {
        await Assert.That("List`1".FirstToken('`')).IsEqualTo("List");
        await Assert.That("noDelimiter".FirstToken('.')).IsEqualTo("noDelimiter");
        await Assert.That("a.b.c".LastToken('.')).IsEqualTo("c");
        await Assert.That("noDelimiter".LastToken('.')).IsEqualTo("noDelimiter");
    }

    [Test]
    public async Task ObjectExtensions_property_access()
    {
        var obj = new SamplePoco { Name = "test", Count = 3 };
        await Assert.That(obj.HasProperty("Name")).IsTrue();
        await Assert.That(obj.HasProperty("Missing")).IsFalse();
        await Assert.That(obj.TryGetPropertyValue("Name", out string? name)).IsTrue();
        await Assert.That(name).IsEqualTo("test");
        await Assert.That(obj.TryGetPropertyValue("Missing", out string? missing)).IsFalse();
        await Assert.That(((object?)null).HasProperty("Name")).IsFalse();
    }

    sealed class SampleMethods
    {
        public SampleMethods(int id) => Id = id;
        public int Id { get; }
        public int Add(int a, string b) => a + b.Length + Id;
    }

    sealed class SamplePoco
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
