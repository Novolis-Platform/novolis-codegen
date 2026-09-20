# Getting started

Codegen pipeline, reflection, and binding generators used across the platform.

Published guide: [https://novolis-platform.github.io/.github/novolis-codegen/](https://novolis-platform.github.io/.github/novolis-codegen/)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GitHub Packages auth for `Novolis.*` (see [nuget-only-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/nuget-only-policy.md))

Configure GPR once from a sibling `novolis-governance` checkout:

```powershell
pwsh -File d:\novolis\novolis-governance\scripts\configure-gpr-user-nuget.ps1
```

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings
```

Local multi-repo iteration uses ProjectReference mode via `d:\novolis\Novolis.Platform.slnx` — never a local NuGet folder feed.

## C-ABI binding emitters

`Novolis.CodeGen.Bindings` emits C ABI interop and thin C# façades. It supports native C libraries and C++ libraries behind an `extern "C"` shim; it does not bind C++ classes directly.

Describe exported functions with typed signatures, including the generated parameter names:

```csharp
var imports = new InteropExportsFragment(
    Id: "example",
    SchemaVersion: 1,
    Header: null,
    Description: "Example C ABI.",
    DllName: "example",
    Policy: new InteropPolicySpec([], [], null, UseDisableRuntimeMarshalling: true),
    Structs: [],
    Imports:
    [
        new InteropImportSpec(
            "example_open",
            NativeSignature.Create(
                NativeType.NativeInt,
                new NativeParameter("path", NativeType.Utf8String))),
    ],
    Usings: ["Example.Interop"]);
```

Register `LibraryImportEmitter`, `DynamicExportsEmitter`, or `FacadeForwardEmitter` as `BindingEmitJob`s, then run them through `BindingCodegenHost<TPhase, TContext>` from `Novolis.CodeGen.Bindings.Roslyn`. Keep the generated source in a T1 structural-equivalence test when replacing an existing emitter.

## Next

- [design.md](design.md) — layer placement and non-goals
- [release.md](release.md) — publish cadence
- [Org docs catalog](https://novolis-platform.github.io/.github/)
