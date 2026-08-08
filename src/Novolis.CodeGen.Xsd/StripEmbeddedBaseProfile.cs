using System.Collections.Frozen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Novolis.CodeGen.Xsd;

/// <summary>
/// Emits StripEmbedded Base records + interfaces (<c>*Base</c> / <c>I*Base</c>),
/// rewriting BinaryFacet embeddings to metadata-only <c>BinaryObjectRef</c> records.
/// Optional shared spine via <see cref="EmitOptions.SpineInterfaceName"/> + <see cref="EmitOptions.SpineDocumentRootNames"/>.
/// </summary>
public sealed class StripEmbeddedBaseProfile : IEmitProfile
{
    /// <inheritdoc />
    public string Name => "StripEmbeddedBase";

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
            ["dateTime"] = "DateTime",
            ["date"] = "DateTime",
            ["time"] = "DateTime"
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenSet<string> ReservedPropNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "object", "string", "int", "class", "event", "params", "base", "this"
        }.ToFrozenSet(StringComparer.Ordinal);

    /// <inheritdoc />
    public EmitResult Emit(SchemaGraph graph, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var include = options.IncludeTypeIds;
        var complex = include is null
            ? graph.ComplexTypes
            : graph.ComplexTypes.Where(t => include.Contains(t.Id)).ToArray();

        var rootTypeIds = graph.DocumentRoots
            .Where(e => e.TypeId is not null)
            .Select(e => e.TypeId!.Value)
            .ToHashSet();

        var rootLocalByType = graph.DocumentRoots
            .Where(e => e.TypeId is not null)
            .GroupBy(e => e.TypeId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var emitTypes = complex
            .Where(t => !ShouldSkipTypeEmit(t))
            .ToArray();

        var files = new List<EmittedFile>
        {
            new("BinaryObjectRef.g.cs", BuildBinaryObjectRef(options.RootNamespace))
        };

        var propsByType = new Dictionary<SchemaTypeId, PropSpec[]>();
        foreach (var type in emitTypes)
        {
            var isRoot = rootTypeIds.Contains(type.Id);
            var props = BuildProperties(graph, type, options, isRoot, rootLocalByType).ToArray();
            props = Deduplicate(props, BaseTypeName(type, isRoot, rootLocalByType));
            propsByType[type.Id] = props;
        }

        string? spineName = options.SpineInterfaceName;
        var spineRoots = options.SpineDocumentRootNames;
        PropSpec[]? spineProps = null;
        if (!string.IsNullOrEmpty(spineName) && spineRoots is { Count: > 0 })
        {
            var spineDocTypes = emitTypes
                .Where(t => rootTypeIds.Contains(t.Id)
                            && rootLocalByType.TryGetValue(t.Id, out var ln)
                            && spineRoots.Contains(ln))
                .ToArray();
            if (spineDocTypes.Length >= 2)
            {
                spineProps = ComputeIntersection(spineDocTypes.Select(t => propsByType[t.Id]).ToArray());
                files.Add(new EmittedFile($"{spineName}.g.cs",
                    BuildSpineInterface(options.RootNamespace, spineName!, spineProps)));
            }
        }

        foreach (var type in emitTypes)
        {
            var isRoot = rootTypeIds.Contains(type.Id);
            var typeName = BaseTypeName(type, isRoot, rootLocalByType);
            var ifaceName = "I" + typeName;
            var props = propsByType[type.Id];
            var extendSpine = spineProps is not null
                              && isRoot
                              && rootLocalByType.TryGetValue(type.Id, out var docName)
                              && spineRoots is not null
                              && spineRoots.Contains(docName)
                              && !string.IsNullOrEmpty(spineName);

            var cu = BuildTypeUnit(
                options.RootNamespace,
                typeName,
                ifaceName,
                props,
                extendSpine ? spineName : null,
                extendSpine ? spineProps : null);
            files.Add(new EmittedFile($"{typeName}.g.cs", cu));
        }

        return new EmitResult(files);
    }

    private static bool ShouldSkipTypeEmit(ComplexTypeNode type)
    {
        if (type.BinaryFacet is BinaryFacet.Base64Binary or BinaryFacet.BinaryObject)
            return true;
        if (IsCollapsibleScalar(type))
            return true;
        return false;
    }

    private static bool IsCollapsibleScalar(ComplexTypeNode type)
    {
        // Keep types with attributes (currencyID, schemeID, …) as Base records so metadata survives.
        if (type.Attributes.Count > 0)
            return false;
        if (type.HasSimpleContent && type.Particle is null)
            return true;
        if (type.Particle is null && type.SimpleContentClrType is not null)
            return true;
        return false;
    }

    private static string BaseTypeName(
        ComplexTypeNode type,
        bool isDocumentRoot,
        IReadOnlyDictionary<SchemaTypeId, string> rootLocalByType)
    {
        if (isDocumentRoot && rootLocalByType.TryGetValue(type.Id, out var local))
            return local + "Base";

        var name = type.CSharpName;
        if (name.EndsWith("Type", StringComparison.Ordinal) && name.Length > 4)
            return name[..^4] + "Base";
        return name + "Base";
    }

    private sealed record PropSpec(string Name, string TypeName, bool IsOptional);

    private static IEnumerable<PropSpec> BuildProperties(
        SchemaGraph graph,
        ComplexTypeNode type,
        EmitOptions options,
        bool isDocumentRoot,
        IReadOnlyDictionary<SchemaTypeId, string> rootLocalByType)
    {
        foreach (var attr in type.Attributes)
        {
            var resolved = ResolvePropertyType(graph, attr.TypeId, options, rootLocalByType, collection: false, optional: !attr.IsRequired);
            if (resolved is null)
                continue;
            yield return new PropSpec(Sanitize(attr.Name), resolved, !attr.IsRequired);
        }

        if (type.HasSimpleContent || type.BinaryFacet != BinaryFacet.None)
        {
            var clr = type.SimpleContentClrType
                      ?? (type.BinaryFacet != BinaryFacet.None ? "byte[]" : "string");
            if (clr == "byte[]")
            {
                if (options.StripEmbeddedPolicy != StripEmbeddedPolicy.Omit)
                    yield return new PropSpec("Value", "BinaryObjectRef", IsOptional: true);
            }
            else
            {
                yield return new PropSpec("Value", clr, IsOptional: false);
            }
        }

        if (type.Particle is { } particle)
        {
            foreach (var p in FlattenElements(graph, particle, options, rootLocalByType))
                yield return p;
        }
    }

    private static IEnumerable<PropSpec> FlattenElements(
        SchemaGraph graph,
        Particle particle,
        EmitOptions options,
        IReadOnlyDictionary<SchemaTypeId, string> rootLocalByType)
    {
        if (particle.Kind == ParticleKind.Any)
        {
            var anyType = particle.IsCollection
                ? "IReadOnlyList<string>"
                : "string?";
            yield return new PropSpec(Sanitize(particle.ElementName ?? "Any"), anyType, particle.MinOccurs == 0);
            yield break;
        }

        if (particle.Kind == ParticleKind.Element)
        {
            var resolved = ResolvePropertyType(
                graph,
                particle.TypeId,
                options,
                rootLocalByType,
                collection: particle.IsCollection,
                optional: particle.MinOccurs == 0 && !particle.IsCollection);
            if (resolved is not null)
                yield return new PropSpec(Sanitize(particle.ElementName ?? "Item"), resolved, particle.MinOccurs == 0 && !particle.IsCollection);
            yield break;
        }

        foreach (var child in particle.Children)
        {
            foreach (var p in FlattenElements(graph, child, options, rootLocalByType))
                yield return p;
        }
    }

    private static string? ResolvePropertyType(
        SchemaGraph graph,
        SchemaTypeId? typeId,
        EmitOptions options,
        IReadOnlyDictionary<SchemaTypeId, string> rootLocalByType,
        bool collection,
        bool optional)
    {
        if (typeId is null)
            return Wrap("string", collection, optional);

        if (typeId.Value.NamespaceName == "http://www.w3.org/2001/XMLSchema")
        {
            var builtin = MapXsdBuiltin(typeId.Value.LocalName);
            if (builtin == "byte[]")
                return options.StripEmbeddedPolicy == StripEmbeddedPolicy.Omit
                    ? null
                    : Wrap("BinaryObjectRef", collection, optional);
            return Wrap(builtin, collection, optional);
        }

        if (graph.SimpleById.TryGetValue(typeId.Value, out var simple))
        {
            if (simple.BinaryFacet != BinaryFacet.None)
            {
                return options.StripEmbeddedPolicy == StripEmbeddedPolicy.Omit
                    ? null
                    : Wrap("BinaryObjectRef", collection, optional);
            }

            return Wrap(simple.ClrTypeName, collection, optional);
        }

        if (graph.ComplexById.TryGetValue(typeId.Value, out var complex))
        {
            if (complex.BinaryFacet != BinaryFacet.None)
            {
                return options.StripEmbeddedPolicy == StripEmbeddedPolicy.Omit
                    ? null
                    : Wrap("BinaryObjectRef", collection, optional);
            }

            if (IsCollapsibleScalar(complex))
            {
                var clr = complex.SimpleContentClrType ?? "string";
                if (clr == "byte[]")
                {
                    return options.StripEmbeddedPolicy == StripEmbeddedPolicy.Omit
                        ? null
                        : Wrap("BinaryObjectRef", collection, optional);
                }

                return Wrap(clr, collection, optional);
            }

            var isRoot = rootLocalByType.ContainsKey(complex.Id);
            var baseName = BaseTypeName(complex, isRoot, rootLocalByType);
            return Wrap(baseName, collection, optional);
        }

        // Unknown / excluded (e.g. XmlDsig) — use string placeholder
        return Wrap("string", collection, optional);
    }

    private static string MapXsdBuiltin(string local) =>
        XsdBuiltins.TryGetValue(local, out var clr) ? clr : "string";

    private static string Wrap(string clr, bool collection, bool optional)
    {
        if (collection)
            return $"IReadOnlyList<{clr}>";
        if (optional && clr is not "string" and not "byte[]")
            return clr + "?";
        if (optional && clr == "string")
            return "string?";
        return clr;
    }

    private static PropSpec[] Deduplicate(PropSpec[] props, string enclosingName)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { enclosingName };
        var result = new List<PropSpec>(props.Length);
        foreach (var p in props)
        {
            var name = p.Name;
            if (string.Equals(name, enclosingName, StringComparison.Ordinal))
                name += "Value";
            var n = 2;
            while (!used.Add(name))
            {
                name = p.Name + n;
                n++;
            }

            result.Add(p with { Name = name });
        }

        return result.ToArray();
    }

    private static PropSpec[] ComputeIntersection(PropSpec[][] sets)
    {
        if (sets.Length == 0)
            return [];

        var first = sets[0].ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
        foreach (var set in sets.Skip(1))
        {
            var names = set.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var key in first.Keys.ToArray())
            {
                if (!names.Contains(key))
                    first.Remove(key);
                else
                {
                    var other = set.First(p => p.Name == key);
                    if (!string.Equals(first[key].TypeName, other.TypeName, StringComparison.Ordinal))
                        first.Remove(key);
                }
            }
        }

        return first.Values.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
    }

    private static CompilationUnitSyntax BuildBinaryObjectRef(string ns)
    {
        var parameters = new[]
        {
            Parameter(Identifier("MimeCode")).WithType(ParseTypeName("string?")),
            Parameter(Identifier("Filename")).WithType(ParseTypeName("string?")),
            Parameter(Identifier("Uri")).WithType(ParseTypeName("string?")),
            Parameter(Identifier("Format")).WithType(ParseTypeName("string?")),
            Parameter(Identifier("EncodingCode")).WithType(ParseTypeName("string?")),
            Parameter(Identifier("CharacterSetCode")).WithType(ParseTypeName("string?"))
        };

        var record = RecordDeclaration(Token(SyntaxKind.RecordKeyword), "BinaryObjectRef")
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        return CompilationUnit()
            .WithUsings(List(new[]
            {
                UsingDirective(ParseName("System"))
            }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(record)))
            .WithLeadingTrivia(NullableEnable())
            .NormalizeWhitespace();
    }

    private static CompilationUnitSyntax BuildSpineInterface(string ns, string name, PropSpec[] props)
    {
        var members = props
            .Select(p => (MemberDeclarationSyntax)PropertyDeclaration(ParseTypeName(p.TypeName), p.Name)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))))
            .ToArray();

        var iface = InterfaceDeclaration(name)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithMembers(List(members));

        return CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Collections.Generic")))
            .AddMembers(FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(iface))
            .WithLeadingTrivia(NullableEnable())
            .NormalizeWhitespace();
    }

    private static CompilationUnitSyntax BuildTypeUnit(
        string ns,
        string typeName,
        string ifaceName,
        PropSpec[] props,
        string? spineInterface,
        PropSpec[]? spineProps)
    {
        var spineNames = spineProps?.Select(p => p.Name).ToHashSet(StringComparer.Ordinal)
                         ?? new HashSet<string>(StringComparer.Ordinal);
        var ifacePropList = spineInterface is null
            ? props
            : props.Where(p => !spineNames.Contains(p.Name)).ToArray();

        var ifaceMembers = ifacePropList
            .Select(p => (MemberDeclarationSyntax)PropertyDeclaration(ParseTypeName(p.TypeName), p.Name)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))))
            .ToArray();

        var iface = InterfaceDeclaration(ifaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithMembers(List(ifaceMembers));

        if (spineInterface is not null)
        {
            iface = iface.WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(IdentifierName(spineInterface)))));
        }

        var parameters = props
            .Select(p => Parameter(Identifier(p.Name)).WithType(ParseTypeName(p.TypeName)))
            .ToArray();

        var record = RecordDeclaration(Token(SyntaxKind.RecordKeyword), typeName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(IdentifierName(ifaceName)))))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        return CompilationUnit()
            .AddUsings(
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Collections.Generic")))
            .AddMembers(FileScopedNamespaceDeclaration(ParseName(ns)).AddMembers(iface, record))
            .WithLeadingTrivia(NullableEnable())
            .NormalizeWhitespace();
    }

    private static SyntaxTriviaList NullableEnable() =>
        TriviaList(
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
            EndOfLine("\n"));

    private static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Item";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var s = new string(chars);
        if (char.IsDigit(s[0]))
            s = "_" + s;
        if (ReservedPropNames.Contains(s))
            s += "Value";
        return s;
    }
}
