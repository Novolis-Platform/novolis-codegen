namespace Novolis.CodeGen.Xml;

/// <summary>Particle compositor kind.</summary>
public enum ParticleKind
{
    /// <summary>Ordered sequence.</summary>
    Sequence,

    /// <summary>Choice among children.</summary>
    Choice,

    /// <summary>Unordered all.</summary>
    All,

    /// <summary>Element reference particle.</summary>
    Element,

    /// <summary>Wildcard <c>xs:any</c> particle.</summary>
    Any
}
