<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Reflection.ClassDiagram

Build Mermaid `classDiagram` text from reflected types in an assembly — type names, constructors, and diagram structure for docs and diagnostics.

## Install

```bash
dotnet add package Novolis.CodeGen.Reflection.ClassDiagram
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), `Novolis.CodeGen.Reflection` (friendly/display name helpers).

## Quick start

```csharp
using System.Reflection;
using Novolis.CodeGen.Reflection.ClassDiagram;

Assembly assembly = typeof(MyService).Assembly;
var diagram = new ClassDiagramBuilder(assembly).Build();
// classDiagram
// class MyService
//     MyService()
```

Output is Mermaid source suitable for Markdown docs or CI-generated diagrams. The legacy `Novolis.CodeGen.Reflection.Mermaid` namespace is obsolete — use this package instead.

## Quick start — interface-driven

```csharp
IClassDiagramBuilder builder = new ClassDiagramBuilder(assembly);
string mermaid = builder.Build();
```

## API

| Type | Role |
|------|------|
| `ClassDiagramBuilder` | `Build()` → Mermaid `classDiagram` for all assembly types |
| `IClassDiagramBuilder` | Diagram builder contract |
| `IDiagramBuilder` | Base `Build()` diagram contract |

Type and member labels use `GetFriendlyName()` / `GetDisplayName()` from `Novolis.CodeGen.Reflection`.

## Support

Pre-release platform library. Diagrams list all public types in the assembly; filter at the call site if needed.

## Related

| Package | Role |
|---------|------|
| `Novolis.CodeGen.Reflection` | Friendly and display type names |
| `Novolis.CodeGen.Reflection.Dump` | Emit C# initialization syntax from runtime objects |
| `Novolis.CodeGen.Pipeline` | Multi-step codegen pipelines |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)

