using Novolis.CodeGen.Reflection;
using TUnit.Core;

namespace Novolis.CodeGen.Reflection.Tests;

public class TypeExtensionsTests
{
    [Test]
    public async Task GetDisplayName_ReturnsCorrectDisplayNameForSimpleType()
    {
        var displayName = typeof(int).GetDisplayName();
        await Assert.That(displayName).IsEqualTo("Integer");
    }

    [Test]
    public async Task GetDisplayName_ReturnsCorrectDisplayNameForGenericType()
    {
        var displayName = typeof(Dictionary<string, object>).GetDisplayName();
        await Assert.That(displayName).IsEqualTo("DictionaryOfStringAndObject");
    }

    [Test]
    public async Task GetFullDisplayName_ReturnsCorrectFullDisplayNameForSimpleType()
    {
        var fullDisplayName = typeof(int).GetFullDisplayName();
        await Assert.That(fullDisplayName).IsEqualTo("System.Integer");
    }

    [Test]
    public async Task GetFullDisplayName_ReturnsCorrectFullDisplayNameForGenericType()
    {
        var fullDisplayName = typeof(Dictionary<string, object>).GetFullDisplayName();
        await Assert.That(fullDisplayName).IsEqualTo("System.Collections.Generic.DictionaryOfStringAndObject");
    }

    [Test]
    public async Task GetFriendlyName_ReturnsCorrectFriendlyNameForSimpleType()
    {
        var friendlyName = typeof(int).GetFriendlyName();
        await Assert.That(friendlyName).IsEqualTo("Integer");
    }

    [Test]
    public async Task GetFriendlyName_ReturnsCorrectFriendlyNameForGenericType()
    {
        var friendlyName = typeof(Dictionary<string, object>).GetFriendlyName();
        await Assert.That(friendlyName).IsEqualTo("Dictionary<String, Object>");
    }

    [Test]
    public async Task GetFullFriendlyName_ReturnsCorrectFullFriendlyNameForSimpleType()
    {
        var fullFriendlyName = typeof(int).GetFullFriendlyName();
        await Assert.That(fullFriendlyName).IsEqualTo("System.Integer");
    }

    [Test]
    public async Task GetFullFriendlyName_ReturnsCorrectFullFriendlyNameForGenericType()
    {
        var fullFriendlyName = typeof(Dictionary<string, object>).GetFullFriendlyName();
        await Assert.That(fullFriendlyName).IsEqualTo("System.Collections.Generic.Dictionary<String, Object>");
    }
}
