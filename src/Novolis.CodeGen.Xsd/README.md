# Novolis.CodeGen.Xsd

Public face for **SchemaGraph → C#**: choose a profile, mold via `EmitOptions` / hooks, write sources.

Orchestration across many regen steps (fingerprint skip, `result.json`) stays in `Novolis.CodeGen.Pipeline` — implement `IPipelineStep` that calls `XsdCodegen.Emit`.

## Install

```bash
dotnet add package Novolis.CodeGen.Xsd
```

## Quick start

```csharp
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

var result = XsdCodegen.EmitFromDirectory(
    schemaRoot,
    new WireXmlSerializerProfile(),
    new EmitOptions
    {
        RootNamespace = "Acme.Schemas",
        NamespaceMapper = new DefaultNamespaceMapper(),
        DocumentRootInterfaceName = "IDocument",
        EnableNullable = true, // default: #nullable enable + ? on optional / choice particles
    });
```

## Mold points (`EmitOptions`)

| Option | Role |
|--------|------|
| `EnableNullable` | `#nullable enable` + `?` on optional attributes/elements (default **true**) |
| `NamespaceMapper` | XML URI → C# namespace (default: last URI segment) |
| `DocumentRootInterfaceName` | Shared interface on Wire document roots |
| `SpineInterfaceName` + `SpineDocumentRootNames` | Shared Base spine over named document roots |
| `StripEmbeddedPolicy` | BinaryFacet → metadata-only vs omit |
| `Hooks` | Ordered `IXsdEmitHook` transforms after the profile emits |

Product conventions (UBL CBC suffixes, SBDH at package root, Invoice/CreditNote/Reminder spine) live in **consumers** (e.g. `Novolis.Xsd.Generator`), not in this package.

## Profiles

- **WireXmlSerializerProfile** — `partial class` + XmlSerializer attributes + interfaces
- **LeanRecordsProfile** — records without embedded `byte[]`
- **StripEmbeddedBaseProfile** — `*Base` records + `I*Base`; optional shared spine when roots are listed
