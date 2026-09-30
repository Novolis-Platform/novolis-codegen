using Microsoft.CodeAnalysis.CSharp.Syntax;
using Novolis.CodeGen.Xml;

namespace Novolis.CodeGen.Xsd;

/// <summary>How binary embeddings are handled in Base/Lean emit.</summary>
public enum StripEmbeddedPolicy
{
    /// <summary>Omit byte[] Value; keep mime/filename/uri metadata as <c>BinaryObjectRef</c>.</summary>
    MetadataOnly = 0,

    /// <summary>Omit binary properties entirely.</summary>
    Omit = 1
}
