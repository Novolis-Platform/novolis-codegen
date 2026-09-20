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
        Imports:
        [
            new InteropImportSpec(
                "Init",
                NativeSignature.Create(
                    NativeType.Void,
                    new NativeParameter("flags", NativeType.UInt32))),
        ],
        Usings: ["Example.Interop"]));

var options = BindingCodegenOptions.Physical(
    @"d:\repo",
    manifests,
    "dotnet run --project codegen/Example.CodeGen -- generate");
```

Use `LibraryImportEmitter` for `InteropExportsFragment`, `DynamicExportsEmitter` for `ShimExportsFragment`, and `FacadeForwardEmitter` for one `FacadeTypesFragment` slice. `NativeSignature` carries parameter names as well as types, so consumer manifests control the emitted C# ABI exactly.

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

