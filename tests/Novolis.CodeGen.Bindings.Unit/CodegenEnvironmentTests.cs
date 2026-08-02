using Novolis.CodeGen.Bindings;
using System.IO.Abstractions.TestingHelpers;

namespace Novolis.CodeGen.Bindings.Unit;

public sealed class CodegenEnvironmentTests
{
    [Test]
    public async Task CodegenEnvironment_reads_and_writes_bytes()
    {
        const string repoRoot = @"C:\novolis\codegen-env";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), repoRoot);
        var env = new CodegenEnvironment { FileSystem = fileSystem, RepoRoot = repoRoot };

        env.WriteAllText("src/input.txt", "hello");
        await Assert.That(env.FileExists("src/input.txt")).IsTrue();
        await Assert.That(env.ReadAllText("src/input.txt")).IsEqualTo("hello");
        var fileBytes = env.ReadAllBytes("src/input.txt");
        await Assert.That(fileBytes.Length).IsEqualTo(5);
        await Assert.That(System.Text.Encoding.UTF8.GetString(fileBytes)).IsEqualTo("hello");
        await Assert.That(env.Combine("generated", "out.cs")).IsEqualTo(@"C:\novolis\codegen-env\generated\out.cs");

        env.WriteAllTextAbsolute(@"C:\novolis\codegen-env\abs\file.cs", "// abs");
        await Assert.That(fileSystem.File.ReadAllText(@"C:\novolis\codegen-env\abs\file.cs")).IsEqualTo("// abs");
    }

    [Test]
    public async Task Physical_factory_uses_real_filesystem()
    {
        var temp = Directory.CreateTempSubdirectory("novolis-codegen-env-");
        try
        {
            var env = CodegenEnvironment.Physical(temp.FullName);
            env.WriteAllText("x.txt", "data");
            await Assert.That(env.FileExists("x.txt")).IsTrue();
            await Assert.That(File.ReadAllText(Path.Combine(temp.FullName, "x.txt"))).IsEqualTo("data");
        }
        finally
        {
            temp.Delete(true);
        }
    }
}
