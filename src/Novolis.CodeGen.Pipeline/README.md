<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Pipeline

Linear **step orchestration** kernel: fingerprinted skip detection, `step.log`, and `result.json` caching.

This package is schema-agnostic. It does **not** emit XSD/C# — pair it with `Novolis.CodeGen.Xsd` (`XsdCodegen.Emit` inside an `IPipelineStep`) or binding hosts (raylib, etc.).

| Concern | Package |
|---------|---------|
| Skip/cache multi-step regen | **Pipeline** (this) |
| SchemaGraph → C# profiles + mold hooks | `Novolis.CodeGen.Xsd` |
| Load / filter XSD → SchemaGraph | `Novolis.CodeGen.Xml` |

## Install

```bash
dotnet add package Novolis.CodeGen.Pipeline
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

Implement `IPipelineStep` and `IPipelineLayout`, then run a profile:

```csharp
using Novolis.CodeGen.Pipeline;

var layout = new MyPipelineLayout(repoRoot);
var runner = new PipelineRunner(steps, layout);
var exit = await runner.RunProfileAsync(["step_01_source", "step_06_codegen"], force: false);
```

Steps declare `InputPaths` and `ExpectedOutputPaths` for `StepSkipEvaluator` to skip unchanged work.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Xsd` | XSD SchemaGraph emit (`XsdCodegen`) from a pipeline step |
| `Novolis.CodeGen.Bindings` | Binding emit steps inside codegen pipelines |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).
