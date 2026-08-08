using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Novolis.CodeGen.Xsd;

/// <summary>Emits lean records omitting BinaryFacet / byte[] embeddings.</summary>
public sealed class LeanRecordsProfile : IEmitProfile
{
    /// <inheritdoc />
    public string Name => "LeanRecords";

    /// <inheritdoc />
    public EmitResult Emit(SchemaGraph graph, EmitOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);

        var include = options.IncludeTypeIds;
        var complex = include is null
            ? graph.ComplexTypes
            : graph.ComplexTypes.Where(t => include.Contains(t.Id)).ToArray();

        var files = new List<EmittedFile>();
        foreach (var type in complex)
        {
            if (type.BinaryFacet is BinaryFacet.Base64Binary or BinaryFacet.BinaryObject)
                continue; // strip embedded binary types entirely from lean graph

            var cu = BuildRecord(graph, type, options);
            files.Add(new EmittedFile($"{type.CSharpName}.Lean.g.cs", cu));
        }

        return new EmitResult(files);
    }

    private static CompilationUnitSyntax BuildRecord(SchemaGraph graph, ComplexTypeNode type, EmitOptions options)
    {
        var parameters = new List<ParameterSyntax>();

        foreach (var attr in type.Attributes)
        {
            var clr = Resolve(graph, attr.TypeId);
            if (clr == "byte[]")
                continue;
            parameters.Add(Parameter(Identifier(Sanitize(attr.Name)))
                .WithType(ParseTypeName(clr)));
        }

        if (type.Particle is { } particle)
            CollectElements(graph, particle, parameters);

        var record = RecordDeclaration(Token(SyntaxKind.RecordKeyword), type.CSharpName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(ParameterList(SeparatedList(parameters)))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        var ns = FileScopedNamespaceDeclaration(ParseName(options.RootNamespace))
            .AddMembers(record);

        return CompilationUnit()
            .WithUsings(List(new[]
            {
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("System.Collections.Generic"))
            }))
            .AddMembers(ns)
            .NormalizeWhitespace();
    }

    private static void CollectElements(SchemaGraph graph, Particle particle, List<ParameterSyntax> parameters)
    {
        if (particle.Kind == ParticleKind.Element)
        {
            var clr = Resolve(graph, particle.TypeId);
            if (clr == "byte[]")
                return;
            if (particle.IsCollection)
                clr = $"IReadOnlyList<{clr}>";
            else if (particle.MinOccurs == 0)
                clr += "?";

            parameters.Add(Parameter(Identifier(Sanitize(particle.ElementName ?? "Item")))
                .WithType(ParseTypeName(clr)));
            return;
        }

        foreach (var child in particle.Children)
            CollectElements(graph, child, parameters);
    }

    private static string Resolve(SchemaGraph graph, SchemaTypeId? typeId)
    {
        if (typeId is null)
            return "string";
        if (graph.SimpleById.TryGetValue(typeId.Value, out var s))
            return s.BinaryFacet != BinaryFacet.None ? "byte[]" : s.ClrTypeName;
        if (graph.ComplexById.TryGetValue(typeId.Value, out var c))
        {
            if (c.BinaryFacet != BinaryFacet.None)
                return "byte[]";
            return c.CSharpName;
        }

        if (typeId.Value.NamespaceName == "http://www.w3.org/2001/XMLSchema"
            && typeId.Value.LocalName is "base64Binary" or "hexBinary")
            return "byte[]";

        return Sanitize(typeId.Value.LocalName);
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Item";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
        var s = new string(chars);
        return char.IsDigit(s[0]) ? "_" + s : s;
    }
}
