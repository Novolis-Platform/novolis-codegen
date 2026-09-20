<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Bindings

Typed C-ABI manifests, shared string emitters, and filesystem-backed binding codegen for Novolis consumers.

The library binds C ABIs: native C libraries and C++ libraries that expose an unmangled `extern "C"` surface (usually through a shim). It does not generate bindings for C++ classes, overloads, exceptions, or ownership models.

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

1. Author typed signatures (`NativeSignature` / `NativeParameter`).
2. Put them in an `InteropExportsFragment` (and optional `FacadeTypesFragment`).
3. Register jobs with `BindingEmitJob.LibraryImport` / `FacadeForward` / `DynamicExports`.
4. Call `BindingCodegen.Generate` from `Novolis.CodeGen.Bindings.Roslyn`.

```csharp
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;

var manifests = BindingManifestSource.Create(
    new InteropExportsFragment(
        Id: "mylib",
        SchemaVersion: 1,
        Header: null,
        Description: null,
        DllName: "mylib",
        Policy: new InteropPolicySpec([], [], "AggressiveInlining", UseDisableRuntimeMarshalling: true),
        Structs: [],
        Imports:
        [
            new InteropImportSpec(
                "Init",
                NativeSignature.Create(
                    NativeType.Void,
                    new NativeParameter("flags", NativeType.UInt32))),
        ]));

var options = BindingCodegenOptions.Physical(
    @"d:\repo",
    manifests,
    "dotnet run --project codegen/Example.CodeGen -- generate");

var project = BindingProject.Create("MyLib")
    .AddJob(BindingEmitJob.LibraryImport(
        "interop",
        "mylib",
        "MyLibNative",
        "src/MyLib.Bindings/Interop/MyLibNative.g.cs",
        "MyLib.Interop",
        "MyLib.Bindings",
        libraryConstantName: "MyLibDll"));

BindingCodegen.Generate(project, options);
```

`NativeSignature` carries parameter names as well as types, so consumer manifests control the emitted C# ABI exactly. Use the generic `BindingCodegenHost<TPhase, TContext>` only when you need Roslyn hooks (Raylib EndDrawing rewrite, docs injection, and similar).

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Bindings.Roslyn` | Default `BindingCodegen.Generate`, hooks, formatting, and structural parity comparison |
| `Novolis.CodeGen.Pipeline` | Step runner with fingerprinted skip/cache for maintainer pipelines |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Binding codegen spec](../../docs/specs/binding-codegen-library/initial-idea-v2.md)
- TinyExpr lab: `d:\novolis\novolis-lab\labs\codegen\TinyExprBindings\`

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
