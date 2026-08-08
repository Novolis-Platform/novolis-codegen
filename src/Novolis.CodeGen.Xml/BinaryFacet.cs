namespace Novolis.CodeGen.Xml;

/// <summary>Marks types whose content model carries binary payloads (base64Binary / BinaryObject-like).</summary>
public enum BinaryFacet
{
    /// <summary>Not a binary content type.</summary>
    None = 0,

    /// <summary>XSD <c>base64Binary</c> or equivalent.</summary>
    Base64Binary = 1,

    /// <summary>UBL-style BinaryObject / Attachment with embedded bytes.</summary>
    BinaryObject = 2
}
