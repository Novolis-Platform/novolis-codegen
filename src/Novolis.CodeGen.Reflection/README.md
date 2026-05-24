# Novolis.CodeGen.Reflection

Type display helpers for reflection-based codegen and diagnostics.

## Install

```bash
dotnet add package Novolis.CodeGen.Reflection
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.CodeGen.Reflection;

var name = typeof(Dictionary<string, object>).GetFriendlyName();
// Dictionary<string, object>

var display = typeof(Dictionary<string, object>).GetDisplayName();
// DictionaryOfStringAndObject
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Reflection.Dump` | Emit C# initialization syntax from runtime objects |
| `Novolis.CodeGen.Reflection.ClassDiagram` | Build Mermaid class diagrams from types |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
