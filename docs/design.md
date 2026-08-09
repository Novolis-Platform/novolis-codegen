# Design

Codegen pipeline, reflection, and binding generators used across the platform.

Published docs: [https://novolis-platform.github.io/.github/novolis-codegen/](https://novolis-platform.github.io/.github/novolis-codegen/)

## Layer placement

Follow [library-boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md) for layer placement.

## Goals

- Keep public APIs documented and packable as `Novolis.*` on GitHub Packages (when applicable).
- Prefer BCL types and existing Novolis packages over parallel abstractions.
- Document restore and ProjectReference-mode builds without local NuGet folder feeds.

## Non-goals

- Local NuGet folder feeds or committed cross-repo `ProjectReference` into sibling checkouts.
- Avalonia package references outside `Novolis.Avalonia.*`.
- Upward spine dependencies (e.g. Math → Simulation).

## Packages

- `Novolis.CodeGen.Bindings`
- `Novolis.CodeGen.Bindings.Roslyn`
- `Novolis.CodeGen.Pipeline`
- `Novolis.CodeGen.Reflection`
- `Novolis.CodeGen.Reflection.ClassDiagram`
- `Novolis.CodeGen.Reflection.Dump`
- `Novolis.CodeGen.Xml`
- `Novolis.CodeGen.Xsd`

## Topics

- `dotnet`
- `codegen`
- `novolis`
