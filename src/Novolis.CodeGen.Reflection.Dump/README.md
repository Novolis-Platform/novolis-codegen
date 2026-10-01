<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-codegen/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-codegen/) · [Source](https://github.com/Novolis-Platform/novolis-codegen)
<!-- novolis-pkg-brand:end -->

# Novolis.CodeGen.Reflection.Dump

Var-dump style helpers that emit C# initialization or declaration syntax from runtime objects.

## Install

```bash
dotnet add package Novolis.CodeGen.Reflection.Dump
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.CodeGen.Reflection.Dump;

var source = myObject.DumpVar();
var classSource = myObject.DumpClass();

// Persist dumps as .cs files (e.g. trained ML snapshots as fixtures)
var store = new DumpFileStore(@"d:\data\dumps");
await store.SaveClassAsync("best-policy", myObject);
await myObject.DumpClassToFileAsync(@"d:\data\dumps\best-policy.cs");
```

Useful for tests, debugging, scaffolding codegen fixtures from live instances, and storing model snapshots as compilable C#.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Reflection` | Type name formatting helpers |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).

