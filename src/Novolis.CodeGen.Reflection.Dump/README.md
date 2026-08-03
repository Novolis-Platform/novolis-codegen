<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-codegen">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
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
```

Useful for tests, debugging, and scaffolding codegen fixtures from live instances.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.CodeGen.Reflection` | Type name formatting helpers |

## More documentation

- [Getting started](../../docs/getting-started.md)

## Support

Pre-release platform library. Public API is fully documented with strict XML (`CS1591` enforced).

