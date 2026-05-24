# Novolis.CodeGen.Reflection.ClassDiagram

Build Mermaid `classDiagram` text from reflected types.

## Install

```bash
dotnet add package Novolis.CodeGen.Reflection.ClassDiagram
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.CodeGen.Reflection.ClassDiagram;

var diagram = new ClassDiagramBuilder()
    .AddType(typeof(MyService))
    .Build();
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Reflection` | Friendly and display type names |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. The legacy `Novolis.CodeGen.Reflection.Mermaid` namespace is obsolete.
