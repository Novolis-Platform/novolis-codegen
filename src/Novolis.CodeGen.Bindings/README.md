# Novolis.CodeGen.Bindings

Manifest fragments, emit orchestration, and filesystem-backed binding codegen for Novolis consumers.

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

Define C# manifest fragments, expose them through `IBindingManifestSource`, and run a consumer host:

```csharp
using Novolis.CodeGen.Bindings;

var manifests = BindingManifestSource.Create(
    new InteropExportsFragment(
        Id: "mylib",
        SchemaVersion: 1,
        Header: null,
        Description: null,
        DllName: "mylib.dll",
        Policy: new InteropPolicySpec([], [], null, false),
        Structs: [],
        Imports: [new InteropImportSpec("Init", "void_void")]));

var options = BindingCodegenOptions.Physical("/path/to/repo", manifests);
// Consumer IBindingCodegenHost implementation calls emitters with options.Environment (IFileSystem).
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Bindings.Roslyn` | Roslyn hooks, formatting, and structural parity comparison |
| `Novolis.CodeGen.Pipeline` | Step runner with fingerprinted skip/cache for maintainer pipelines |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Binding codegen spec](../../docs/specs/binding-codegen-library/initial-idea-v2.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
