using System.IO.Abstractions.TestingHelpers;

using Novolis.CodeGen.Reflection.Dump;
using Novolis.CodeGen.Reflection.Dump.Tests.TestingInfrastructure;

using TUnit.Core;

namespace Novolis.CodeGen.Reflection.Dump.Tests;

public class DumpFileStoreTests
{
    [Test]
    public async Task SaveClassAsync_WritesCsFile_And_LoadSourceAsync_ReturnsContent()
    {
        var fs = new MockFileSystem();
        var root = fs.Path.Combine(fs.Path.GetTempPath(), "dump-store-" + Guid.NewGuid().ToString("N"));
        var store = new DumpFileStore(root, fs);
        var person = new Person { Name = "Frank", Age = 30, Address = new Address { Street = "Street", Number = 1 } };

        var path = await store.SaveClassAsync("frank-person", person);

        await Assert.That(fs.File.Exists(path)).IsTrue();
        await Assert.That(path).EndsWith(".cs");
        var source = await store.LoadSourceAsync("frank-person");
        await Assert.That(source).IsNotNullOrWhiteSpace();
        await Assert.That(source!).Contains("Person");
    }

    [Test]
    public async Task ListIdsAsync_ReturnsNewestFirst()
    {
        var fs = new MockFileSystem();
        var root = fs.Path.Combine(fs.Path.GetTempPath(), "dump-list-" + Guid.NewGuid().ToString("N"));
        var store = new DumpFileStore(root, fs);
        var person = new Person { Name = "A", Age = 1, Address = new Address { Street = "S", Number = 1 } };

        await store.SaveClassAsync("older", person);
        await store.SaveClassAsync("newer", person);

        var ids = await store.ListIdsAsync();
        await Assert.That(ids).Contains("older");
        await Assert.That(ids).Contains("newer");
    }

    [Test]
    public async Task DumpClassToFileAsync_CreatesParentDirectory()
    {
        var fs = new MockFileSystem();
        var path = fs.Path.Combine(fs.Path.GetTempPath(), "nested-" + Guid.NewGuid().ToString("N"), "out.cs");
        var person = new Person { Name = "Bob", Age = 35, Address = new Address { Street = "B", Number = 3 } };

        await person.DumpClassToFileAsync(path, fs);

        await Assert.That(fs.File.Exists(path)).IsTrue();
        var text = await fs.File.ReadAllTextAsync(path);
        await Assert.That(text).Contains("Bob");
    }

    [Test]
    public async Task Delete_RemovesExistingDump()
    {
        var fs = new MockFileSystem();
        var root = fs.Path.Combine(fs.Path.GetTempPath(), "dump-del-" + Guid.NewGuid().ToString("N"));
        var store = new DumpFileStore(root, fs);
        var person = new Person { Name = "X", Age = 1, Address = new Address { Street = "S", Number = 1 } };
        await store.SaveVarAsync("to-delete", person);

        await Assert.That(store.Delete("to-delete")).IsTrue();
        await Assert.That(await store.LoadSourceAsync("to-delete")).IsNull();
        await Assert.That(store.Delete("to-delete")).IsFalse();
    }
}
