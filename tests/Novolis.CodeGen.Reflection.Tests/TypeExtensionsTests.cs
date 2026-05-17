using FluentAssertions;
using Novolis.CodeGen.Reflection;

namespace Novolis.CodeGen.Reflection.Tests;

public class TypeExtensionsTests
{
    [Test]
    public void GetDisplayName_ReturnsCorrectDisplayNameForSimpleType()
    {
        var displayName = typeof(int).GetDisplayName();
        displayName.Should().Be("Integer");
    }

    [Test]
    public void GetDisplayName_ReturnsCorrectDisplayNameForGenericType()
    {
        var displayName = typeof(Dictionary<string, object>).GetDisplayName();
        displayName.Should().Be("DictionaryOfStringAndObject");
    }

    [Test]
    public void GetFullDisplayName_ReturnsCorrectFullDisplayNameForSimpleType()
    {
        var fullDisplayName = typeof(int).GetFullDisplayName();
        fullDisplayName.Should().Be("System.Integer");
    }

    [Test]
    public void GetFullDisplayName_ReturnsCorrectFullDisplayNameForGenericType()
    {
        var fullDisplayName = typeof(Dictionary<string, object>).GetFullDisplayName();
        fullDisplayName.Should().Be("System.Collections.Generic.DictionaryOfStringAndObject");
    }

    [Test]
    public void GetFriendlyName_ReturnsCorrectFriendlyNameForSimpleType()
    {
        var friendlyName = typeof(int).GetFriendlyName();
        friendlyName.Should().Be("Integer");
    }

    [Test]
    public void GetFriendlyName_ReturnsCorrectFriendlyNameForGenericType()
    {
        var friendlyName = typeof(Dictionary<string, object>).GetFriendlyName();
        friendlyName.Should().BeEquivalentTo("Dictionary<string, object>");
    }

    [Test]
    public void GetFullFriendlyName_ReturnsCorrectFullFriendlyNameForSimpleType()
    {
        var fullFriendlyName = typeof(int).GetFullFriendlyName();
        fullFriendlyName.Should().Be("System.Integer");
    }

    [Test]
    public void GetFullFriendlyName_ReturnsCorrectFullFriendlyNameForGenericType()
    {
        var fullFriendlyName = typeof(Dictionary<string, object>).GetFullFriendlyName();
        fullFriendlyName.Should().BeEquivalentTo("System.Collections.Generic.Dictionary<string, object>");
    }
}
