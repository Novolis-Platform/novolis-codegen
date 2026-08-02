using Novolis.CodeGen.Reflection.Dump;
using Novolis.CodeGen.Reflection.Dump.Tests.TestingInfrastructure;
using TUnit.Core;

namespace Novolis.CodeGen.Reflection.Dump.Tests;

public class DumpExtensionsTests
{
    [Test]
    public async Task Dump()
    {
        var data = new Person
        {
            Name = "Frank",
            Age = 30,
            Address = new Address { Street = "Street", Number = 1 }
        };

        var dump = data.DumpClass();
        TestContext.Current?.OutputWriter.WriteLine(dump);
        await Assert.That(dump).IsNotNullOrWhiteSpace();
    }

    [Test]
    public async Task DumpVar()
    {
        var data = new Person
        {
            Name = "Frank",
            Age = 30,
            Address = new Address { Street = "Street", Number = 1 }
        };

        var dump = data.DumpVar();
        TestContext.Current?.OutputWriter.WriteLine(dump);
        await Assert.That(dump).IsNotNullOrWhiteSpace();
    }

    [Test]
    public async Task DumpEnumerable()
    {
        var people = new List<Person>
        {
            new() { Name = "Frank", Age = 30, Address = new Address { Street = "Street", Number = 1 } },
            new() { Name = "Alice", Age = 25, Address = new Address { Street = "Avenue", Number = 2 } },
            new() { Name = "Bob", Age = 35, Address = new Address { Street = "Boulevard", Number = 3 } }
        };

        var result = people.DumpEnumerable(p => p.Name);
        TestContext.Current?.OutputWriter.WriteLine(result);

        await Assert.That(result).Contains("public class People : IEnumerable<Person>");
        await Assert.That(result).Contains("yield return GetFrank();");
        await Assert.That(result).Contains("yield return GetAlice();");
        await Assert.That(result).Contains("yield return GetBob();");
        await Assert.That(result).Contains("public static Person GetFrank()");
        await Assert.That(result).Contains("public static Person GetAlice()");
        await Assert.That(result).Contains("public static Person GetBob()");
    }

    [Test]
    public async Task DumpMethod_and_Roslyn_syntax_helpers()
    {
        var data = new Person
        {
            Name = "Frank",
            Age = 30,
            Address = new Address { Street = "Street", Number = 1 },
        };

        var method = data.DumpMethod();
        await Assert.That(method).Contains("Person");

        var classDecl = DumpExtensions.DumpClassDeclarationSyntax(data);
        await Assert.That(classDecl.Identifier.Text).Contains("Person");

        var methodDecl = DumpExtensions.DumpMethodDeclarationSyntax(data);
        await Assert.That(methodDecl.ReturnType).IsNotNull();
    }
}
