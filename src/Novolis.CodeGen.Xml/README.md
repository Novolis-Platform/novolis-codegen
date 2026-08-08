# Novolis.CodeGen.Xml

Loads and compiles XSD into an immutable **SchemaGraph** IR for downstream emitters (`Novolis.CodeGen.Xsd`).

## Install

```bash
dotnet add package Novolis.CodeGen.Xml
```

## Quick start

```csharp
using Novolis.CodeGen.Xml;

var graph = SchemaGraphBuilder.BuildFromDirectory(xsdRoot);
```

## Capabilities

- Directory load with DTD parse + cleared `schemaLocation` (UBL-style shared imports)
- Complex/simple types, particles, attributes, binary facet tagging
- Deterministic type ordering for stable emit
