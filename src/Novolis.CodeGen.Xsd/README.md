# Novolis.CodeGen.Xsd

Emits C# from a **SchemaGraph** using Roslyn `SyntaxFactory` (no CodeDom).

## Profiles

- **WireXmlSerializerProfile** — `partial class` + XmlSerializer attributes + interfaces
- **LeanRecordsProfile** — records without embedded `byte[]` (BinaryFacet → omit / BlobRef)
- **StripEmbeddedBaseProfile** — `*Base` records + `I*Base` interfaces; `BinaryObjectRef` metadata only; optional `IBillingDocumentBase` spine for Invoice/CreditNote/Reminder

## NuGet

```xml
<PackageReference Include="Novolis.CodeGen.Xsd" Version="2026.1.*" />
```
