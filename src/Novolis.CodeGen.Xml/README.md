# Novolis.CodeGen.Xml

Loads and compiles XSD into an immutable **SchemaGraph** IR for downstream emitters (`Novolis.CodeGen.Xsd`).

## Capabilities

- Directory load with DTD parse + cleared `schemaLocation` (UBL-style shared imports)
- Complex/simple types, particles, attributes, binary facet tagging
- Deterministic type ordering for stable emit

## NuGet

```xml
<PackageReference Include="Novolis.CodeGen.Xml" Version="2026.1.*" />
```
