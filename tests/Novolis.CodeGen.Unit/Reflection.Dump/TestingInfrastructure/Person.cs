namespace Novolis.CodeGen.Reflection.Dump.Tests.TestingInfrastructure;

public class Person
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public Address Address { get; set; } = new();
}
