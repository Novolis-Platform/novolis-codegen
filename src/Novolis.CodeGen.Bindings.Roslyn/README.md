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

## Quick start

Run a declared `BindingProject` through the standard string-emission, parse, hook, format, and write path:

```csharp
using Novolis.CodeGen.Bindings;
using Novolis.CodeGen.Bindings.Roslyn;

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

Jobs own their source-format policy: `RoslynFormatter` for interop, shims, and debug output; `NormalizeWhitespace` for façade forwards. Use `CompilationUnitComparer.AreStructurallyEquivalent` for T1 parity gates between committed and emitted source.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Bindings` | Manifest fragments, `BindingEmitContext`, and emit orchestration |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Binding codegen spec](../../docs/specs/binding-codegen-library/initial-idea-v2.md)

## Support

Pre-release platform library. Depends on `Microsoft.CodeAnalysis.CSharp`.

