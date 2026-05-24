using Novolis.CodeGen.Bindings;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class CodegenEnvironmentTests
{
    [Test]
    public async Task WriteAllText_uses_virtual_file_system()
    {
        const string repoRoot = @"C:\novolis\codegen-test";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var env = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = repoRoot };

        env.WriteAllText("generated/Example.g.cs", "// generated");

        await Assert.That(fileSystem.File.Exists(@"C:\novolis\codegen-test\generated\Example.g.cs")).IsTrue();
        await Assert.That(fileSystem.File.ReadAllText(@"C:\novolis\codegen-test\generated\Example.g.cs"))
            .IsEqualTo("// generated");
    }
}
