<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Bindings.Roslyn

Roslyn hook host, default binding-job runner, emit writer, and structural compilation-unit comparison for C-ABI binding codegen.

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings.Roslyn
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start (no hooks)

Most C libraries only need typed manifests and jobs — no phase enum and no `CreateContext` plumbing:

```csharp
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;

var options = BindingCodegenOptions.Physical(
    repoRoot,
    manifests,
    "dotnet run --project My.CodeGen -- generate");

var project = BindingProject.Create("MyLib")
    .AddJob(BindingEmitJob.LibraryImport(
        "interop",
        fragmentId: "mylib",
        className: "MyLibNative",
        relativePath: "src/MyLib.Bindings/Interop/MyLibNative.g.cs",
        namespaceName: "MyLib.Interop",
        assemblyName: "MyLib.Bindings",
        libraryConstantName: "MyLibDll"))
    .AddJob(BindingEmitJob.FacadeForward(
        "facade",
        fragmentId: "facades",
        typeName: "AudioDevice",
        relativePath: "src/MyLib.Runtime/Audio/AudioDevice.g.cs",
        namespaceName: "MyLib",
        assemblyName: "MyLib.Runtime",
        facadeMethodImpl: "AggressiveInlining"));

var exit = BindingCodegen.Generate(project, options);
```

See the TinyExpr lab for a complete tiny consumer:
`d:\novolis\novolis-lab\labs\codegen\TinyExprBindings\`.

## Custom phases and hooks

Raylib-style consumers that rewrite syntax after emit still use the generic host:

```csharp
var exit = new BindingCodegenHost<MyPhase, MyContext>().Generate(
    new BindingCodegenRun<MyPhase, MyContext>
    {
        Project = project,
        Options = options,
        SelectPhase = job => MyPhase.Emit,
        CreateContext = (job, fragment, outputPath, fingerprint) =>
            new MyContext
            {
                Environment = options.Environment,
                OutputPath = outputPath,
                Fragment = fragment,
                ManifestSha256 = fingerprint,
                RegenerateHint = options.RegenerateHint,
            },
        Hooks = HookDiscovery.Discover<MyPhase, MyContext>(typeof(MyEndDrawingHook).Assembly),
    });
```

Jobs own their source-format policy: `RoslynFormatter` for interop, shims, and debug output; `NormalizeWhitespace` for façade forwards (applied automatically by `BindingEmitJob.FacadeForward`). Use `CompilationUnitComparer.AreStructurallyEquivalent` for T1 parity gates between committed and emitted source.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Bindings` | Manifest fragments, `BindingEmitJob` factories, and emit orchestration |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Binding codegen spec](../../docs/specs/binding-codegen-library/initial-idea-v2.md)

## Support

Pre-release platform library. Depends on `Microsoft.CodeAnalysis.CSharp`.
