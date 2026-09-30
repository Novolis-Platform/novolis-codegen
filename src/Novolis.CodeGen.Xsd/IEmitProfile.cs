using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>Emits C# syntax from a <see cref="SchemaGraph"/>.</summary>
public interface IEmitProfile
{
    /// <summary>Profile name for logging.</summary>
    string Name { get; }

    /// <summary>Emits one compilation unit (or multiple files via <see cref="EmitResult"/>).</summary>
    EmitResult Emit(SchemaGraph graph, EmitOptions options);
}
