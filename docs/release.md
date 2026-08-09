# Release

This repository publishes with the org CalVer scheme (`2026.1.*`) via `merge.yml` to GitHub Packages when packages are packable.

See [release-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md).

Published docs: [https://novolis-platform.github.io/.github/novolis-codegen/](https://novolis-platform.github.io/.github/novolis-codegen/)

## Packages

- `Novolis.CodeGen.Bindings`
- `Novolis.CodeGen.Bindings.Roslyn`
- `Novolis.CodeGen.Pipeline`
- `Novolis.CodeGen.Reflection`
- `Novolis.CodeGen.Reflection.ClassDiagram`
- `Novolis.CodeGen.Reflection.Dump`
- `Novolis.CodeGen.Xml`
- `Novolis.CodeGen.Xsd`

## Consumers

Restore from nuget.org + `https://nuget.pkg.github.com/Novolis-Platform/index.json` only.

Local multi-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode) — do not add a local feed.
