<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Pipeline

Linear step pipeline with fingerprinted skip detection, `step.log`, and `result.json` caching.

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
| `Novolis.CodeGen.Bindings` | Binding emit steps inside codegen pipelines |

## More documentation

- [Getting started](../../docs/getting-started.md)
- [Design](../../docs/design.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).

