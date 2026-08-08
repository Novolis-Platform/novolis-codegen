using System.Xml;
using System.Xml.Schema;

namespace Novolis.CodeGen.Xml;

/// <summary>Builds an immutable <see cref="SchemaGraph"/> from a compiled <see cref="XmlSchemaSet"/>.</summary>
public static class SchemaGraphBuilder
{
    private static readonly XmlQualifiedName XsString = new("string", XmlSchema.Namespace);
    private static readonly XmlQualifiedName XsBase64 = new("base64Binary", XmlSchema.Namespace);
    private static readonly XmlQualifiedName XsHex = new("hexBinary", XmlSchema.Namespace);

    /// <summary>Builds a graph from a compiled schema set.</summary>
    public static SchemaGraph Build(XmlSchemaSet schemaSet, SchemaGraphOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(schemaSet);
        options ??= new SchemaGraphOptions();

        var referencedElementNames = new HashSet<XmlQualifiedName>();
        var complex = new Dictionary<SchemaTypeId, ComplexTypeNode>();
        var simple = new Dictionary<SchemaTypeId, SimpleTypeNode>();
        var elements = new List<(XmlSchemaElement Element, SchemaTypeId? TypeId)>();

        foreach (XmlSchemaType? type in schemaSet.GlobalTypes.Values)
        {
            if (type is null)
                continue;
            if (options.ExcludedNamespaces.Contains(type.QualifiedName.Namespace))
                continue;

            switch (type)
            {
                case XmlSchemaComplexType ct:
                    AddComplex(ct, complex, referencedElementNames, options);
                    break;
                case XmlSchemaSimpleType st:
                    AddSimple(st, simple, options);
                    break;
            }
        }

        foreach (XmlSchemaElement? el in schemaSet.GlobalElements.Values)
        {
            if (el is null)
                continue;
            if (options.ExcludedNamespaces.Contains(el.QualifiedName.Namespace))
                continue;

            var typeId = ResolveElementTypeId(el);
            elements.Add((el, typeId));
            if (el.ElementSchemaType is XmlSchemaComplexType anonCt
                && string.IsNullOrEmpty(anonCt.QualifiedName.Name)
                && typeId is { } tid
                && !complex.ContainsKey(tid))
            {
                AddComplex(anonCt, complex, referencedElementNames, options, overrideId: tid);
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
        SchemaGraphOptions options,
        SchemaTypeId? overrideId = null)
    {
        var id = overrideId ?? ToTypeId(ct.QualifiedName, ct);
        if (complex.ContainsKey(id))
            return;
        if (options.ExcludedNamespaces.Contains(id.NamespaceName))
            return;

        var particle = MapParticle(ct.ContentTypeParticle, referencedElementNames);
        var attrs = MapAttributes(ct);
        var baseId = ct.BaseXmlSchemaType is { } bas && bas.QualifiedName.Name is { Length: > 0 }
            ? ToTypeId(bas.QualifiedName, bas)
            : (SchemaTypeId?)null;

        var binary = DetectBinaryFacet(ct);
        var (hasSimple, simpleClr) = DetectSimpleContent(ct, binary);
        complex[id] = new ComplexTypeNode(
            id,
            SanitizeName(id.LocalName),
            particle,
            attrs,
            baseId,
            ct.IsAbstract,
            binary,
            hasSimple,
            simpleClr);
    }

    private static (bool HasSimple, string? Clr) DetectSimpleContent(XmlSchemaComplexType ct, BinaryFacet binary)
    {
        if (ct.ContentType is XmlSchemaContentType.TextOnly or XmlSchemaContentType.Mixed)
        {
            if (binary is BinaryFacet.Base64Binary or BinaryFacet.BinaryObject)
                return (true, "byte[]");
            return (true, MapTypeCode(ct.Datatype?.TypeCode));
        }

        if (ct.ContentModel is XmlSchemaSimpleContent)
        {
            if (binary is BinaryFacet.Base64Binary or BinaryFacet.BinaryObject)
                return (true, "byte[]");
            return (true, MapTypeCode(ct.Datatype?.TypeCode));
        }

        return (false, null);
    }

    private static string MapTypeCode(XmlTypeCode? code) => code switch
    {
        XmlTypeCode.Base64Binary or XmlTypeCode.HexBinary => "byte[]",
        XmlTypeCode.Boolean => "bool",
        XmlTypeCode.Decimal => "decimal",
        XmlTypeCode.Double => "double",
        XmlTypeCode.Float => "float",
        XmlTypeCode.Int or XmlTypeCode.Integer => "int",
        XmlTypeCode.Long => "long",
        XmlTypeCode.Short => "short",
        XmlTypeCode.DateTime or XmlTypeCode.Date or XmlTypeCode.Time => "DateTime",
        _ => "string"
    };

    private static void AddSimple(
        XmlSchemaSimpleType st,
        Dictionary<SchemaTypeId, SimpleTypeNode> simple,
        SchemaGraphOptions options)
    {
        var id = ToTypeId(st.QualifiedName, st);
        if (simple.ContainsKey(id))
            return;
        if (options.ExcludedNamespaces.Contains(id.NamespaceName))
            return;

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
        var qn = st.Datatype?.TypeCode switch
        {
            XmlTypeCode.Base64Binary => (nameof(Byte) + "[]", BinaryFacet.Base64Binary),
            XmlTypeCode.HexBinary => (nameof(Byte) + "[]", BinaryFacet.Base64Binary),
            XmlTypeCode.Boolean => ("bool", BinaryFacet.None),
            XmlTypeCode.Decimal => ("decimal", BinaryFacet.None),
            XmlTypeCode.Double => ("double", BinaryFacet.None),
            XmlTypeCode.Float => ("float", BinaryFacet.None),
            XmlTypeCode.Int or XmlTypeCode.Integer => ("int", BinaryFacet.None),
            XmlTypeCode.Long => ("long", BinaryFacet.None),
            XmlTypeCode.Short => ("short", BinaryFacet.None),
            XmlTypeCode.DateTime or XmlTypeCode.Date or XmlTypeCode.Time => ("DateTime", BinaryFacet.None),
            _ => ("string", BinaryFacet.None)
        };

        // Walk base for base64 when TypeCode is string but named BinaryObject
        if (qn.Item2 == BinaryFacet.None
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

        return qn;
    }

    private static BinaryFacet DetectBinaryFacet(XmlSchemaComplexType ct)
    {
        if (ct.QualifiedName.Name.Contains("BinaryObject", StringComparison.OrdinalIgnoreCase)
            || ct.Name?.Contains("BinaryObject", StringComparison.OrdinalIgnoreCase) == true)
            return BinaryFacet.BinaryObject;

        if (ct.ContentType == XmlSchemaContentType.TextOnly
            && ct.Datatype?.TypeCode is XmlTypeCode.Base64Binary or XmlTypeCode.HexBinary)
            return BinaryFacet.Base64Binary;

        return BinaryFacet.None;
    }

    private static IReadOnlyList<AttributeDecl> MapAttributes(XmlSchemaComplexType ct)
    {
        var list = new List<AttributeDecl>();
        foreach (XmlSchemaObject? obj in ct.AttributeUses.Values)
        {
            if (obj is not XmlSchemaAttribute attr)
                continue;
            var name = attr.QualifiedName.Name;
            if (string.IsNullOrEmpty(name))
                name = attr.Name ?? "attr";
            SchemaTypeId? typeId = null;
            if (attr.AttributeSchemaType is { } at)
                typeId = ToTypeId(at.QualifiedName.Name.Length > 0 ? at.QualifiedName : XsString, at);

            list.Add(new AttributeDecl(
                name,
                string.IsNullOrEmpty(attr.QualifiedName.Namespace) ? null : attr.QualifiedName.Namespace,
                typeId,
                attr.Use == XmlSchemaUse.Required,
                attr.DefaultValue ?? attr.FixedValue));
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
            XmlSchemaSequence seq => MapGroup(ParticleKind.Sequence, seq, referenced),
            XmlSchemaChoice choice => MapGroup(ParticleKind.Choice, choice, referenced),
            XmlSchemaAll all => MapGroup(ParticleKind.All, all, referenced),
            XmlSchemaGroupRef groupRef when groupRef.Particle is { } inner =>
                MapParticle(inner, referenced),
            _ => null
        };
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
            typeId);
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
                return new SchemaTypeId(eq.Namespace ?? string.Empty, eq.Name + "Type");
        }

        if (!el.SchemaTypeName.IsEmpty)
            return new SchemaTypeId(el.SchemaTypeName.Namespace ?? string.Empty, el.SchemaTypeName.Name);

        return null;
    }

    private static SchemaTypeId ToTypeId(XmlQualifiedName qn, XmlSchemaType type)
    {
        if (!qn.IsEmpty && qn.Name.Length > 0)
            return new SchemaTypeId(qn.Namespace ?? string.Empty, qn.Name);

        // Anonymous
        var name = type.Name;
        if (string.IsNullOrEmpty(name))
            name = "Anonymous" + Math.Abs(type.GetHashCode());
        return new SchemaTypeId(type.QualifiedName.Namespace ?? string.Empty, name);
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
