using FluentAssertions;
using Novolis.CodeGen.Reflection.Dump;
using Novolis.CodeGen.Reflection.Dump.Tests.TestingInfrastructure;

namespace Novolis.CodeGen.Reflection.Dump.Tests;

public class DumpExtensionsTests
{
    [Test]
    public void Dump()
    {
        var data = new Person
        {
            Name = "Frank",
            Age = 30,
            Address = new Address { Street = "Street", Number = 1 }
        };

        var dump = data.DumpClass();
        TestContext.Current?.OutputWriter.WriteLine(dump);
        dump.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void DumpVar()
    {
        var data = new Person
        {
            Name = "Frank",
            Age = 30,
            Address = new Address { Street = "Street", Number = 1 }
        };

        var dump = data.DumpVar();
        TestContext.Current?.OutputWriter.WriteLine(dump);
        dump.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void DumpEnumerable()
    {
        var people = new List<Person>
        {
            new() { Name = "Frank", Age = 30, Address = new Address { Street = "Street", Number = 1 } },
            new() { Name = "Alice", Age = 25, Address = new Address { Street = "Avenue", Number = 2 } },
            new() { Name = "Bob", Age = 35, Address = new Address { Street = "Boulevard", Number = 3 } }
        };

        var result = people.DumpEnumerable(p => p.Name);
        TestContext.Current?.OutputWriter.WriteLine(result);

        result.Should().Contain("public class People : IEnumerable<Person>");
        result.Should().Contain("yield return GetFrank();");
        result.Should().Contain("yield return GetAlice();");
        result.Should().Contain("yield return GetBob();");
        result.Should().Contain("public static Person GetFrank()");
        result.Should().Contain("public static Person GetAlice()");
        result.Should().Contain("public static Person GetBob()");
    }
}
