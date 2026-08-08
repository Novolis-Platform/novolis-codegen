# Novolis.CodeGen.Xsd

Emits C# from a **SchemaGraph** using Roslyn `SyntaxFactory` (no CodeDom).

## Install

```bash
dotnet add package Novolis.CodeGen.Xsd
```

## Quick start

```csharp
using Novolis.CodeGen.Xml;
using Novolis.CodeGen.Xsd;

var graph = SchemaGraphBuilder.BuildFromDirectory(xsdRoot);
var result = new WireXmlSerializerProfile().Emit(graph, new EmitOptions());
```

## Profiles

- **WireXmlSerializerProfile** — `partial class` + XmlSerializer attributes + interfaces
- **LeanRecordsProfile** — records without embedded `byte[]` (BinaryFacet → omit / BlobRef)
- **StripEmbeddedBaseProfile** — `*Base` records + `I*Base` interfaces; `BinaryObjectRef` metadata only; optional `IBillingDocumentBase` spine for Invoice/CreditNote/Reminder
