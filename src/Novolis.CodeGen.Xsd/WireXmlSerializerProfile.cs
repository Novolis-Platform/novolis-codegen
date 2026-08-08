using System.Collections.Frozen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Novolis.CodeGen.Xsd;

/// <summary>Emits XmlSerializer-friendly partial classes + interfaces from a SchemaGraph.</summary>
public sealed class WireXmlSerializerProfile : IEmitProfile
{
    private static readonly FrozenDictionary<string, string> XsdBuiltins =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["base64Binary"] = "byte[]",
            ["hexBinary"] = "byte[]",
            ["boolean"] = "bool",
            ["decimal"] = "decimal",
            ["double"] = "double",
            ["float"] = "float",
            ["int"] = "int",
            ["integer"] = "int",
            ["long"] = "long",
            ["short"] = "short",
            ["dateTime"] = "System.DateTime",
            ["date"] = "System.DateTime",
            ["time"] = "System.DateTime",
            ["anyType"] = "System.Xml.XmlElement",
            ["anySimpleType"] = "string"
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenSet<string> ReservedPropNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "object", "string", "int", "class", "event", "params", "base", "this"
        }.ToFrozenSet(StringComparer.Ordinal);

    /// <inheritdoc />
    public string Name => "WireXmlSerializer";

    /// <inheritdoc />
    public EmitResult Emit(SchemaGraph graph, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var include = options.IncludeTypeIds;
        var complex = include is null
            ? graph.ComplexTypes.Where(t => t.Id.NamespaceName != "http://www.w3.org/2001/XMLSchema").ToArray()
            : graph.ComplexTypes.Where(t => include.Contains(t.Id)).ToArray();
        var simple = include is null
            ? graph.SimpleTypes.Where(s => s.Id.NamespaceName != "http://www.w3.org/2001/XMLSchema").ToArray()
            : graph.SimpleTypes.Where(s => include.Contains(s.Id)).ToArray();

        var rootByType = graph.DocumentRoots
            .Where(e => e.TypeId is not null)
            .GroupBy(e => e.TypeId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var files = new List<EmittedFile>();

        if (options.DocumentRootInterfaceName is { } ifaceName)
        {
            files.Add(new EmittedFile(
                $"{ifaceName}.g.cs",
                BuildEmptyInterface(options.RootNamespace, ifaceName, options)));
        }

        foreach (var type in simple)
        {
            var ns = options.NamespaceMapper.Map(options.RootNamespace, type.Id.NamespaceName);
            var cu = BuildSimpleType(type, ns, options);
            files.Add(new EmittedFile($"{SafePath(ns, type.CSharpName)}.g.cs", cu));
        }

        foreach (var type in complex)
        {
            rootByType.TryGetValue(type.Id, out var rootEl);
            var ns = options.NamespaceMapper.Map(options.RootNamespace, type.Id.NamespaceName);
            var cu = BuildComplexType(graph, type, options, ns, rootEl);
            files.Add(new EmittedFile($"{SafePath(ns, type.CSharpName)}.g.cs", cu));
        }

        return new EmitResult(files);
    }

    private static string SafePath(string ns, string typeName)
    {
        var leaf = ns.Contains('.') ? ns[(ns.LastIndexOf('.') + 1)..] : ns;
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            leaf = leaf.Replace(c, '_');
            typeName = typeName.Replace(c, '_');
        }

        return Path.Combine(leaf, typeName);
    }

    private static CompilationUnitSyntax BuildEmptyInterface(string ns, string name, EmitOptions options)
    {
        var iface = InterfaceDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        var cu = CompilationUnit()
            .AddUsings(UsingDirective(ParseName("System")))
            .AddMembers(FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(iface));
        return WithNullable(cu, options).NormalizeWhitespace();
    }

    private static CompilationUnitSyntax BuildSimpleType(SimpleTypeNode type, string ns, EmitOptions options)
    {
        var clr = type.ClrTypeName;
        var prop = PropertyDeclaration(ParseTypeName(clr), "Value")
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("XmlText")))))
            .AddAccessorListAccessors(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

        if (options.EnableNullable && !IsClrValueTypeName(clr))
        {
            prop = prop
                .WithInitializer(EqualsValueClause(ParseExpression("null!")))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }
        var cls = ClassDeclaration(type.CSharpName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
            .AddAttributeLists(XmlTypeAttributeList(type.Id.LocalName, type.Id.NamespaceName))
            .AddMembers(prop);

        var cu = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Xml.Serialization")))
            .AddMembers(FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(cls));
        return WithNullable(cu, options).NormalizeWhitespace();
    }

    private static CompilationUnitSyntax BuildComplexType(
        SchemaGraph graph,
        ComplexTypeNode type,
        EmitOptions options,
        string ns,
        ElementDecl? rootElement)
    {
        var className = type.CSharpName;
        var ifaceName = "I" + className;
        var properties = DeduplicatePropertyNames(BuildProperties(graph, type, options, className).ToArray(), className);

        var ifaceMembers = properties
            .Select(p => WithSummaryDoc(
                (MemberDeclarationSyntax)PropertyDeclaration(p.Type, p.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))),
                p.Documentation))
            .ToArray();

        var iface = WithSummaryDoc(
            InterfaceDeclaration(ifaceName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithMembers(List(ifaceMembers)),
            type.Documentation);

        var classBases = new List<BaseTypeSyntax> { SimpleBaseType(IdentifierName(ifaceName)) };
        if (rootElement is not null && options.DocumentRootInterfaceName is { } docIface)
            classBases.Add(SimpleBaseType(ParseTypeName("global::" + options.RootNamespace + "." + docIface)));

        var classAttrs = new List<AttributeListSyntax>
        {
            XmlTypeAttributeList(type.Id.LocalName, type.Id.NamespaceName)
        };

        if (rootElement is not null)
            classAttrs.Add(XmlRootAttributeList(rootElement.Name, rootElement.NamespaceName));

        var classMembers = new List<MemberDeclarationSyntax>();
        foreach (var p in properties)
        {
            var prop = PropertyDeclaration(p.Type, p.Name)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

            if (p.XmlAttributes.Count > 0)
                prop = prop.WithAttributeLists(List(p.XmlAttributes));

            prop = WithPropertyInitializer(prop, p);
            classMembers.Add(WithSummaryDoc(prop, p.Documentation));
        }

        var cls = WithSummaryDoc(
            ClassDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
                .WithAttributeLists(List(classAttrs))
                .WithBaseList(BaseList(SeparatedList(classBases)))
                .WithMembers(List(classMembers)),
            type.Documentation);

        var cu = CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Collections.Generic")),
                UsingDirective(ParseName("System.Collections.ObjectModel")),
                UsingDirective(ParseName("System.Xml")),
                UsingDirective(ParseName("System.Xml.Serialization")))
            .AddMembers(FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(iface, cls));
        return WithNullable(cu, options).NormalizeWhitespace();
    }

    private static CompilationUnitSyntax WithNullable(CompilationUnitSyntax cu, EmitOptions options) =>
        options.EnableNullable ? cu.WithLeadingTrivia(EmitNullability.EnableDirective()) : cu;

    private static PropSpec[] DeduplicatePropertyNames(PropSpec[] properties, string enclosingTypeName)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { enclosingTypeName };
        var result = new PropSpec[properties.Length];
        for (var i = 0; i < properties.Length; i++)
        {
            var p = properties[i];
            var name = p.Name;
            var n = 2;
            while (!used.Add(name))
            {
                name = p.Name + n;
                n++;
            }

            result[i] = p with { Name = name };
        }

        return result;
    }

    private static AttributeListSyntax XmlTypeAttributeList(string localName, string namespaceName) =>
        AttributeList(SingletonSeparatedList(
            Attribute(IdentifierName("XmlType"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
            {
                AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(localName))),
                NamespaceNamedArgument(namespaceName)
            })))));

    private static AttributeListSyntax XmlRootAttributeList(string localName, string namespaceName) =>
        AttributeList(SingletonSeparatedList(
            Attribute(IdentifierName("XmlRoot"), AttributeArgumentList(SeparatedList(new AttributeArgumentSyntax[]
            {
                AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(localName))),
                NamespaceNamedArgument(namespaceName)
            })))));

    private static AttributeArgumentSyntax NamespaceNamedArgument(string namespaceName) =>
        AttributeArgument(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName("Namespace"),
                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(namespaceName))));

    private sealed record PropSpec(
        string Name,
        TypeSyntax Type,
        List<AttributeListSyntax> XmlAttributes,
        string? Documentation = null,
        bool IsOptional = false,
        bool IsCollection = false,
        string? ClrElementType = null);

    private static PropertyDeclarationSyntax WithPropertyInitializer(PropertyDeclarationSyntax prop, PropSpec p)
    {
        // XmlSerializer types are activated via parameterless ctor; satisfy CS8618 without lying about optionality.
        if (p.IsCollection && !p.IsOptional)
        {
            return prop
                .WithInitializer(EqualsValueClause(ImplicitObjectCreationExpression()))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        if (p.IsOptional)
            return prop;

        var clr = p.ClrElementType;
        if (string.IsNullOrEmpty(clr) || IsClrValueTypeName(clr))
            return prop;

        return prop
            .WithInitializer(EqualsValueClause(ParseExpression("null!")))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static bool IsClrValueTypeName(string clr)
    {
        var t = clr.EndsWith('?') ? clr[..^1] : clr;
        return t is "bool" or "byte" or "sbyte" or "short" or "ushort" or "int" or "uint"
            or "long" or "ulong" or "float" or "double" or "decimal" or "char"
            or "System.DateTime" or "DateTime"
            or "System.DateTimeOffset" or "DateTimeOffset"
            or "System.Guid" or "Guid"
            or "System.TimeSpan" or "TimeSpan";
    }
    private static IEnumerable<PropSpec> BuildProperties(
        SchemaGraph graph,
        ComplexTypeNode type,
        EmitOptions options,
        string enclosingTypeName)
    {
        foreach (var attr in type.Attributes)
        {
            var clr = ResolveClrType(graph, attr.TypeId, options);
            var optional = options.EnableNullable && !attr.IsRequired;
            var xmlAttr = AttributeList(SingletonSeparatedList(
                Attribute(IdentifierName("XmlAttribute"))
                    .WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                        AttributeArgument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(attr.Name))))))));
            yield return new PropSpec(
                SanitizeProp(attr.Name, enclosingTypeName),
                EmitNullability.ParseAnnotated(clr, optional, collection: false),
                [xmlAttr],
                attr.Documentation,
                IsOptional: optional,
                IsCollection: false,
                ClrElementType: clr);
        }

        if (type.HasSimpleContent || type.BinaryFacet != BinaryFacet.None)
        {
            var clr = type.SimpleContentClrType
                      ?? (type.BinaryFacet != BinaryFacet.None ? "byte[]" : "string");
            var xmlText = Attribute(IdentifierName("XmlText"));
            if (!string.IsNullOrEmpty(type.SimpleContentXmlDataType))
            {
                xmlText = xmlText.WithArgumentList(AttributeArgumentList(SingletonSeparatedList(
                    AttributeArgument(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("DataType"),
                            LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(type.SimpleContentXmlDataType!)))))));
            }

            yield return new PropSpec(
                SanitizeProp("Value", enclosingTypeName),
                ParseTypeName(clr),
                [AttributeList(SingletonSeparatedList(xmlText))],
                Documentation: null,
                IsOptional: false,
                IsCollection: false,
                ClrElementType: clr);
        }

        if (type.Particle is { } particle)
        {
            foreach (var prop in FlattenElements(graph, particle, options, enclosingTypeName, ancestorOptional: false, inChoice: false))
                yield return prop;
        }
    }

    private static IEnumerable<PropSpec> FlattenElements(
        SchemaGraph graph,
        Particle particle,
        EmitOptions options,
        string enclosingTypeName,
        bool ancestorOptional,
        bool inChoice)
    {
        var optionalHere = ancestorOptional || particle.MinOccurs == 0;

        if (particle.Kind == ParticleKind.Any)
        {
            yield return AnyProp(particle, enclosingTypeName, options, optionalHere || inChoice);
            yield break;
        }

        if (particle.Kind == ParticleKind.Element)
        {
            yield return ElementProp(graph, particle, options, enclosingTypeName, optionalHere || inChoice);
            yield break;
        }

        var childInChoice = inChoice || particle.Kind == ParticleKind.Choice;
        foreach (var child in particle.Children)
        {
            foreach (var p in FlattenElements(graph, child, options, enclosingTypeName, optionalHere, childInChoice))
                yield return p;
        }
    }

    private static PropSpec AnyProp(Particle particle, string enclosingTypeName, EmitOptions options, bool optional)
    {
        var annotate = options.EnableNullable && optional;
        TypeSyntax typeSyntax = particle.IsCollection
            ? EmitNullability.ParseAnnotated("System.Xml.XmlElement", annotate, collection: true, options.CollectionTypeName)
            : EmitNullability.ParseAnnotated("System.Xml.XmlElement", annotate, collection: false);

        var xmlAny = AttributeList(SingletonSeparatedList(Attribute(IdentifierName("XmlAnyElement"))));
        return new PropSpec(
            SanitizeProp("Any", enclosingTypeName),
            typeSyntax,
            [xmlAny],
            particle.Documentation,
            IsOptional: annotate,
            IsCollection: particle.IsCollection,
            ClrElementType: "System.Xml.XmlElement");
    }

    private static PropSpec ElementProp(
        SchemaGraph graph,
        Particle particle,
        EmitOptions options,
        string enclosingTypeName,
        bool optional)
    {
        var name = particle.ElementName ?? "Item";
        var clr = ResolveClrType(graph, particle.TypeId, options);
        var annotate = options.EnableNullable && optional;
        var typeSyntax = EmitNullability.ParseAnnotated(
            clr,
            annotate,
            particle.IsCollection,
            options.CollectionTypeName);

        var args = new List<AttributeArgumentSyntax>
        {
            AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(name)))
        };
        if (!string.IsNullOrEmpty(particle.ElementNamespace))
        {
            args.Add(AttributeArgument(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("Namespace"),
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(particle.ElementNamespace!)))));
        }

        if (TryXmlDataTypeForBuiltin(particle.TypeId) is { } dataType)
        {
            args.Add(AttributeArgument(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("DataType"),
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(dataType)))));
        }

        var xmlEl = AttributeList(SingletonSeparatedList(
            Attribute(IdentifierName("XmlElement"))
                .WithArgumentList(AttributeArgumentList(SeparatedList(args)))));

        return new PropSpec(
            SanitizeProp(name, enclosingTypeName),
            typeSyntax,
            [xmlEl],
            particle.Documentation,
            IsOptional: annotate,
            IsCollection: particle.IsCollection,
            ClrElementType: clr);
    }

    private static T WithSummaryDoc<T>(T node, string? documentation) where T : SyntaxNode
    {
        if (string.IsNullOrWhiteSpace(documentation))
            return node;

        var escaped = EscapeXmlDoc(documentation);
        return node.WithLeadingTrivia(ParseLeadingTrivia($"/// <summary>{escaped}</summary>\r\n"));
    }

    private static string EscapeXmlDoc(string text) =>
        text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string? TryXmlDataTypeForBuiltin(SchemaTypeId? typeId)
    {
        if (typeId is null || typeId.Value.NamespaceName != "http://www.w3.org/2001/XMLSchema")
            return null;

        return typeId.Value.LocalName switch
        {
            "date" => "date",
            "time" => "time",
            "dateTime" => "dateTime",
            "base64Binary" => "base64Binary",
            "hexBinary" => "hexBinary",
            "duration" => "duration",
            _ => null
        };
    }

    private static string ResolveClrType(SchemaGraph graph, SchemaTypeId? typeId, EmitOptions options)
    {
        if (typeId is null)
            return "string";

        if (typeId.Value.NamespaceName == "http://www.w3.org/2001/XMLSchema")
            return XsdBuiltins.TryGetValue(typeId.Value.LocalName, out var builtin) ? builtin : "string";

        // Excluded / external schemas (XmlDsig, XAdES) — keep XmlSerializer happy without generating them.
        if (typeId.Value.NamespaceName.Contains("xmldsig", StringComparison.OrdinalIgnoreCase)
            || typeId.Value.NamespaceName.Contains("01903", StringComparison.Ordinal))
        {
            return "System.Xml.XmlElement";
        }

        if (graph.SimpleById.TryGetValue(typeId.Value, out var simple))
        {
            var typeNs = options.NamespaceMapper.Map(options.RootNamespace, simple.Id.NamespaceName);
            return "global::" + typeNs + "." + simple.CSharpName;
        }

        if (graph.ComplexById.TryGetValue(typeId.Value, out var complex))
        {
            var typeNs = options.NamespaceMapper.Map(options.RootNamespace, complex.Id.NamespaceName);
            return "global::" + typeNs + "." + complex.CSharpName;
        }

        var fallbackNs = options.NamespaceMapper.Map(options.RootNamespace, typeId.Value.NamespaceName);
        return "global::" + fallbackNs + "." + SanitizeProp(typeId.Value.LocalName);
    }

    private static string SanitizeProp(string name, string? enclosingTypeName = null)
    {
        if (string.IsNullOrEmpty(name))
            return "Item";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var s = new string(chars);
        if (char.IsDigit(s[0]))
            s = "_" + s;
        if (ReservedPropNames.Contains(s))
            s += "Value";
        if (enclosingTypeName is not null && string.Equals(s, enclosingTypeName, StringComparison.Ordinal))
            s += "Value";
        return s;
    }
}
