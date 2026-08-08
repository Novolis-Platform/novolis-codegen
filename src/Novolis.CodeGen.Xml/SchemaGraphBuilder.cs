using System.Collections.Frozen;
using System.Xml;
using System.Xml.Schema;

namespace Novolis.CodeGen.Xml;

/// <summary>Builds an immutable <see cref="SchemaGraph"/> from a compiled <see cref="XmlSchemaSet"/>.</summary>
public static class SchemaGraphBuilder
{
    private static readonly XmlQualifiedName XsString = new("string", XmlSchema.Namespace);
    private static readonly XmlQualifiedName XsBase64 = new("base64Binary", XmlSchema.Namespace);
    private static readonly XmlQualifiedName XsHex = new("hexBinary", XmlSchema.Namespace);

    private static readonly FrozenDictionary<XmlTypeCode, string> ClrByTypeCode =
        new Dictionary<XmlTypeCode, string>
        {
            [XmlTypeCode.Base64Binary] = "byte[]",
            [XmlTypeCode.HexBinary] = "byte[]",
            [XmlTypeCode.Boolean] = "bool",
            [XmlTypeCode.Decimal] = "decimal",
            [XmlTypeCode.Double] = "double",
            [XmlTypeCode.Float] = "float",
            [XmlTypeCode.Int] = "int",
            [XmlTypeCode.Integer] = "int",
            [XmlTypeCode.Long] = "long",
            [XmlTypeCode.Short] = "short",
            [XmlTypeCode.DateTime] = "DateTime",
            [XmlTypeCode.Date] = "DateTime",
            [XmlTypeCode.Time] = "DateTime"
        }.ToFrozenDictionary();

    /// <summary>Builds a graph from a compiled schema set.</summary>
    public static SchemaGraph Build(XmlSchemaSet schemaSet, SchemaGraphOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schemaSet);
        options ??= new SchemaGraphOptions();

        var referencedElementNames = new HashSet<XmlQualifiedName>();
        var complex = new Dictionary<SchemaTypeId, ComplexTypeNode>();
        var simple = new Dictionary<SchemaTypeId, SimpleTypeNode>();
        var elements = new List<(XmlSchemaElement Element, SchemaTypeId? TypeId)>();

        foreach (XmlSchemaType type in schemaSet.GlobalTypes.Values)
        {
            if (options.ExcludedNamespaces.Contains(type.QualifiedName.Namespace))
                continue;

            switch (type)
            {
                case XmlSchemaComplexType ct:
                    AddComplex(ct, complex, referencedElementNames);
                    break;
                case XmlSchemaSimpleType st:
                    AddSimple(st, simple);
                    break;
            }
        }

        foreach (XmlSchemaElement el in schemaSet.GlobalElements.Values)
        {
            if (options.ExcludedNamespaces.Contains(el.QualifiedName.Namespace))
                continue;

            var typeId = ResolveElementTypeId(el);
            elements.Add((el, typeId));
            if (el.ElementSchemaType is XmlSchemaComplexType anonCt
                && string.IsNullOrEmpty(anonCt.QualifiedName.Name)
                && typeId is { } tid
                && !complex.ContainsKey(tid))
            {
                AddComplex(anonCt, complex, referencedElementNames, overrideId: tid);
            }
        }

        // Second pass: collect particle element refs from all complex types
        foreach (var node in complex.Values.ToArray())
            CollectReferencedElements(node.Particle, referencedElementNames);

        var elementDecls = elements
            .Select(e =>
            {
                var qn = e.Element.QualifiedName;
                var isRoot = IsDocumentRoot(e.Element, referencedElementNames, options);
                return new ElementDecl(
                    qn.Name,
                    qn.Namespace ?? string.Empty,
                    e.TypeId,
                    isRoot,
                    e.Element.IsNillable);
            })
            .OrderBy(e => e.NamespaceName, StringComparer.Ordinal)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();

        var complexOrdered = complex.Values
            .OrderBy(t => t.Id)
            .ToArray();
        var simpleOrdered = simple.Values
            .OrderBy(t => t.Id)
            .ToArray();

        return new SchemaGraph(complexOrdered, simpleOrdered, elementDecls);
    }

    /// <summary>Loads schemas from a directory and builds a graph.</summary>
    public static SchemaGraph BuildFromDirectory(string schemaRoot, SchemaGraphOptions? options = null) =>
        Build(SchemaSetLoader.LoadFromDirectory(schemaRoot), options);

    /// <summary>Loads schemas from files and builds a graph.</summary>
    public static SchemaGraph BuildFromFiles(IEnumerable<string> schemaFiles, SchemaGraphOptions? options = null) =>
        Build(SchemaSetLoader.LoadFromFiles(schemaFiles), options);

    private static bool IsDocumentRoot(
        XmlSchemaElement element,
        HashSet<XmlQualifiedName> referenced,
        SchemaGraphOptions options)
    {
        if (options.DocumentRootLocalNames is { } allow
            && allow.Count > 0
            && !allow.Contains(element.QualifiedName.Name))
        {
            return false;
        }

        if (!options.MarkUnreferencedAsDocumentRoots)
            return options.DocumentRootLocalNames?.Contains(element.QualifiedName.Name) == true;

        return !referenced.Contains(element.QualifiedName);
    }

    private static void AddComplex(
        XmlSchemaComplexType ct,
        Dictionary<SchemaTypeId, ComplexTypeNode> complex,
        HashSet<XmlQualifiedName> referencedElementNames,
        SchemaTypeId? overrideId = null)
    {
        var id = overrideId ?? ToTypeId(ct.QualifiedName, ct);

        var particle = MapParticle(ct.ContentTypeParticle, referencedElementNames);
        var attrs = MapAttributes(ct);
        var baseId = ct.BaseXmlSchemaType is { } bas && bas.QualifiedName.Name is { Length: > 0 }
            ? ToTypeId(bas.QualifiedName, bas)
            : (SchemaTypeId?)null;

        var binary = DetectBinaryFacet(ct);
        var (hasSimple, simpleClr, xmlDataType) = DetectSimpleContent(ct, binary);
        complex[id] = new ComplexTypeNode(
            id,
            SanitizeName(id.LocalName),
            particle,
            attrs,
            baseId,
            ct.IsAbstract,
            binary,
            hasSimple,
            simpleClr,
            xmlDataType,
            SchemaDocumentation.Extract(ct));
    }

    private static (bool HasSimple, string? Clr, string? XmlDataType) DetectSimpleContent(
        XmlSchemaComplexType ct,
        BinaryFacet binary)
    {
        if (ct.ContentType is XmlSchemaContentType.TextOnly or XmlSchemaContentType.Mixed)
        {
            if (binary is BinaryFacet.Base64Binary or BinaryFacet.BinaryObject)
                return (true, "byte[]", MapXmlDataType(ct.Datatype?.TypeCode) ?? "base64Binary");
            return (true, MapTypeCode(ct.Datatype?.TypeCode), MapXmlDataType(ct.Datatype?.TypeCode));
        }

        return (false, null, null);
    }

    private static string? MapXmlDataType(XmlTypeCode? code) =>
        code switch
        {
            XmlTypeCode.Date => "date",
            XmlTypeCode.Time => "time",
            XmlTypeCode.DateTime => "dateTime",
            XmlTypeCode.Base64Binary => "base64Binary",
            XmlTypeCode.HexBinary => "hexBinary",
            XmlTypeCode.Duration => "duration",
            XmlTypeCode.GYear => "gYear",
            XmlTypeCode.GYearMonth => "gYearMonth",
            XmlTypeCode.GMonth => "gMonth",
            XmlTypeCode.GMonthDay => "gMonthDay",
            XmlTypeCode.GDay => "gDay",
            _ => null
        };

    private static string MapTypeCode(XmlTypeCode? code) =>
        code is { } c && ClrByTypeCode.TryGetValue(c, out var clr) ? clr : "string";

    private static void AddSimple(
        XmlSchemaSimpleType st,
        Dictionary<SchemaTypeId, SimpleTypeNode> simple)
    {
        var id = ToTypeId(st.QualifiedName, st);

        var (clr, binary) = MapSimpleClr(st);
        var baseId = st.BaseXmlSchemaType is { } bas && bas.QualifiedName.Name is { Length: > 0 }
            ? ToTypeId(bas.QualifiedName, bas)
            : (SchemaTypeId?)null;

        var enums = ExtractEnumerations(st);
        simple[id] = new SimpleTypeNode(
            id,
            SanitizeName(id.LocalName),
            baseId,
            clr,
            binary,
            enums);
    }

    private static IReadOnlyList<string> ExtractEnumerations(XmlSchemaSimpleType st)
    {
        if (st.Content is not XmlSchemaSimpleTypeRestriction restriction)
            return Array.Empty<string>();

        return restriction.Facets
            .OfType<XmlSchemaEnumerationFacet>()
            .Select(f => f.Value ?? string.Empty)
            .Where(v => v.Length > 0)
            .ToArray();
    }

    private static (string Clr, BinaryFacet Binary) MapSimpleClr(XmlSchemaSimpleType st)
    {
        BinaryFacet binary = BinaryFacet.None;
        var clr = "string";
        if (st.Datatype?.TypeCode is { } code && ClrByTypeCode.TryGetValue(code, out var mapped))
        {
            clr = mapped;
            if (code is XmlTypeCode.Base64Binary or XmlTypeCode.HexBinary)
                binary = BinaryFacet.Base64Binary;
        }

        // Walk base for base64 when TypeCode is string but named BinaryObject
        if (binary == BinaryFacet.None
            && (st.QualifiedName.Name.Contains("Binary", StringComparison.OrdinalIgnoreCase)
                || st.BaseXmlSchemaType?.QualifiedName == XsBase64
                || st.BaseXmlSchemaType?.QualifiedName == XsHex))
        {
            return ("byte[]", st.QualifiedName.Name.Contains("BinaryObject", StringComparison.Ordinal)
                ? BinaryFacet.BinaryObject
                : BinaryFacet.Base64Binary);
        }

        if (st.QualifiedName.Name.Contains("BinaryObject", StringComparison.Ordinal))
            return ("byte[]", BinaryFacet.BinaryObject);

        return (clr, binary);
    }

    private static BinaryFacet DetectBinaryFacet(XmlSchemaComplexType ct)
    {
        if (ct.QualifiedName.Name.Contains("BinaryObject", StringComparison.OrdinalIgnoreCase)
            || ct.Name is not null && ct.Name.Contains("BinaryObject", StringComparison.OrdinalIgnoreCase))
            return BinaryFacet.BinaryObject;

        if (ct.ContentType == XmlSchemaContentType.TextOnly
            && ct.Datatype is { TypeCode: var code }
            && (code == XmlTypeCode.Base64Binary || code == XmlTypeCode.HexBinary))
            return BinaryFacet.Base64Binary;

        return BinaryFacet.None;
    }

    private static IReadOnlyList<AttributeDecl> MapAttributes(XmlSchemaComplexType ct)
    {
        var list = new List<AttributeDecl>();
        foreach (XmlSchemaAttribute attr in ct.AttributeUses.Values.OfType<XmlSchemaAttribute>())
        {
            var name = attr.QualifiedName.Name;
            SchemaTypeId? typeId = attr.AttributeSchemaType is { } at
                ? ToTypeId(string.IsNullOrEmpty(at.QualifiedName.Name) ? XsString : at.QualifiedName, at)
                : null;

            list.Add(new AttributeDecl(
                string.IsNullOrEmpty(name) ? "attr" : name,
                string.IsNullOrEmpty(attr.QualifiedName.Namespace) ? null : attr.QualifiedName.Namespace,
                typeId,
                attr.Use == XmlSchemaUse.Required,
                attr.DefaultValue ?? attr.FixedValue,
                SchemaDocumentation.Extract(attr)));
        }

        return list
            .OrderBy(a => a.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static Particle? MapParticle(XmlSchemaParticle? particle, HashSet<XmlQualifiedName> referenced)
    {
        if (particle is null || particle is XmlSchemaParticle { MaxOccurs: 0 })
            return null;

        return particle switch
        {
            XmlSchemaElement el => MapElementParticle(el, referenced),
            XmlSchemaAny any => MapAnyParticle(any),
            XmlSchemaSequence seq => MapGroup(ParticleKind.Sequence, seq, referenced),
            XmlSchemaChoice choice => MapGroup(ParticleKind.Choice, choice, referenced),
            XmlSchemaAll all => MapGroup(ParticleKind.All, all, referenced),
            // Compiled schemas usually expand group refs; keep a recursive fallback for raw particles.
            XmlSchemaGroupRef groupRef => MapParticle(groupRef.Particle, referenced),
            _ => null
        };
    }

    private static Particle MapAnyParticle(XmlSchemaAny any)
    {
        var max = any.MaxOccursString == "unbounded" ? decimal.MaxValue : any.MaxOccurs;
        return new Particle(ParticleKind.Any, any.MinOccurs, max);
    }

    private static Particle MapElementParticle(XmlSchemaElement el, HashSet<XmlQualifiedName> referenced)
    {
        var qn = el.RefName.IsEmpty ? el.QualifiedName : el.RefName;
        if (!qn.IsEmpty)
            referenced.Add(qn);

        var typeId = ResolveElementTypeId(el);
        var min = el.MinOccurs;
        var max = el.MaxOccursString == "unbounded" ? decimal.MaxValue : el.MaxOccurs;

        return new Particle(
            ParticleKind.Element,
            min,
            max,
            qn.Name,
            qn.Namespace,
            typeId,
            documentation: SchemaDocumentation.Extract(el));
    }

    private static Particle MapGroup(ParticleKind kind, XmlSchemaGroupBase group, HashSet<XmlQualifiedName> referenced)
    {
        var children = new List<Particle>();
        foreach (XmlSchemaObject? item in group.Items)
        {
            if (item is XmlSchemaParticle p)
            {
                var mapped = MapParticle(p, referenced);
                if (mapped is not null)
                    children.Add(mapped);
            }
        }

        var max = group.MaxOccursString == "unbounded" ? decimal.MaxValue : group.MaxOccurs;
        return new Particle(kind, group.MinOccurs, max, children: children);
    }

    private static void CollectReferencedElements(Particle? particle, HashSet<XmlQualifiedName> referenced)
    {
        if (particle is null)
            return;
        if (particle.Kind == ParticleKind.Element
            && particle.ElementName is { } name)
        {
            referenced.Add(new XmlQualifiedName(name, particle.ElementNamespace ?? string.Empty));
        }

        foreach (var child in particle.Children)
            CollectReferencedElements(child, referenced);
    }

    private static SchemaTypeId? ResolveElementTypeId(XmlSchemaElement el)
    {
        if (el.ElementSchemaType is { } est)
        {
            if (!est.QualifiedName.IsEmpty && est.QualifiedName.Name.Length > 0)
                return ToTypeId(est.QualifiedName, est);

            // Anonymous type: synthesize id from element qname
            var eq = el.QualifiedName.IsEmpty ? el.RefName : el.QualifiedName;
            if (!eq.IsEmpty)
                return new SchemaTypeId(eq.Namespace, eq.Name + "Type");
        }

        if (!el.SchemaTypeName.IsEmpty)
            return new SchemaTypeId(el.SchemaTypeName.Namespace, el.SchemaTypeName.Name);

        return null;
    }

    private static SchemaTypeId ToTypeId(XmlQualifiedName qn, XmlSchemaType type)
    {
        if (!qn.IsEmpty && qn.Name.Length > 0)
            return new SchemaTypeId(qn.Namespace, qn.Name);

        // Anonymous
        var name = type.Name;
        if (string.IsNullOrEmpty(name))
            name = "Anonymous" + Math.Abs(type.GetHashCode());
        return new SchemaTypeId(type.QualifiedName.Namespace, name);
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "GeneratedType";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        if (char.IsDigit(chars[0]))
            return "_" + new string(chars);
        return new string(chars);
    }
}
