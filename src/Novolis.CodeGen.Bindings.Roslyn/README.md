# Novolis.CodeGen.Bindings.Roslyn

Roslyn hook host, emit writer, and structural compilation-unit comparison for binding codegen.

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings.Roslyn
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

Register hooks and write formatted output through the virtual filesystem on `BindingEmitContext`:

```csharp
using Novolis.CodeGen.Bindings.Roslyn;

var hooks = HookDiscovery.Discover<MyPhase, MyContext>(typeof(MyEndDrawingHook).Assembly);

RoslynEmitWriter<MyPhase, MyContext>.WriteFile(
    rawSource,
    context,
    MyPhase.Facade,
    hooks,
    FormatPolicy.RoslynFormatter);
```

Use `CompilationUnitComparer.AreStructurallyEquivalent` for T1 parity gates between committed and emitted source.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Bindings` | Manifest fragments, `BindingEmitContext`, and emit orchestration |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Binding codegen spec](../../docs/specs/binding-codegen-library/initial-idea-v2.md)

## Support

Pre-release platform library. Depends on `Microsoft.CodeAnalysis.CSharp`.
