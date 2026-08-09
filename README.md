<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-codegen.svg" width="100%" alt="novolis-codegen"/>
</p>

<p align="center">
  <strong>Bindings pipelines that scale</strong><br/>
  Codegen pipeline, reflection, and binding generators used across the platform.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-codegen/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-codegen/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-codegen/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-codegen"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-codegen/">Docs</a>
  ·
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.CodeGen.Bindings` | `dotnet add package Novolis.CodeGen.Bindings` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Bindings/README.md) |
| `Novolis.CodeGen.Bindings.Roslyn` | `dotnet add package Novolis.CodeGen.Bindings.Roslyn` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Bindings.Roslyn/README.md) |
| `Novolis.CodeGen.Pipeline` | `dotnet add package Novolis.CodeGen.Pipeline` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Pipeline/README.md) |
| `Novolis.CodeGen.Reflection` | `dotnet add package Novolis.CodeGen.Reflection` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Reflection/README.md) |
| `Novolis.CodeGen.Reflection.ClassDiagram` | `dotnet add package Novolis.CodeGen.Reflection.ClassDiagram` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Reflection.ClassDiagram/README.md) |
| `Novolis.CodeGen.Reflection.Dump` | `dotnet add package Novolis.CodeGen.Reflection.Dump` | [README](https://github.com/Novolis-Platform/novolis-codegen/blob/main/src/Novolis.CodeGen.Reflection.Dump/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->
# CodeGen

Reusable binding codegen library for Novolis: manifest fragments, Roslyn emit hooks, and maintainer pipelines.

## Packages

| Package | Purpose |
|---------|---------|
| [Novolis.CodeGen.Bindings](src/Novolis.CodeGen.Bindings/README.md) | C# manifest fragments, `IBindingManifestSource`, `CodegenEnvironment` (`IFileSystem`) |
| [Novolis.CodeGen.Bindings.Roslyn](src/Novolis.CodeGen.Bindings.Roslyn/README.md) | Roslyn hooks, emit writer, T1 structural parity |
| [Novolis.CodeGen.Pipeline](src/Novolis.CodeGen.Pipeline/README.md) | Fingerprinted step runner with skip/cache |
| [Novolis.CodeGen.Reflection](src/Novolis.CodeGen.Reflection/README.md) | Type display helpers |
| [Novolis.CodeGen.Reflection.ClassDiagram](src/Novolis.CodeGen.Reflection.ClassDiagram/README.md) | Mermaid class diagrams |
| [Novolis.CodeGen.Reflection.Dump](src/Novolis.CodeGen.Reflection.Dump/README.md) | Object-to-C# dump helpers |

## Install

```bash
dotnet add package Novolis.CodeGen.Bindings
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## API documentation

All packable libraries under `src/` ship **strict XML API documentation** (`GenerateDocumentationFile` with `CS1591` enforced via [Novolis.Documentation.props](../novolis-governance/build/Novolis.Documentation.props)). IntelliSense and NuGet consumers receive `.xml` doc files alongside each assembly.

## Quick start

See [binding codegen spec v2](docs/specs/binding-codegen-library/initial-idea-v2.md). The raylib consumer (`novolis-raylib`) is the reference integration.

## Documentation

- [Getting started](docs/getting-started.md)
- [Design](docs/design.md)
- [Release](docs/release.md)
- [Binding codegen spec](docs/specs/binding-codegen-library/initial-idea-v2.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Follow [documentation-policy.md](../novolis-governance/docs/documentation-policy.md) for public API `///` comments and package READMEs.

## Security

See [SECURITY.md](SECURITY.md).

