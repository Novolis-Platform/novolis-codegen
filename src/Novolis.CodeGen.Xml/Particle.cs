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
    Element
}

/// <summary>Immutable particle node in a complex type content model.</summary>
public sealed class Particle
{
    /// <summary>Creates a particle.</summary>
    public Particle(
        ParticleKind kind,
        decimal minOccurs,
        decimal maxOccurs,
        string? elementName = null,
        string? elementNamespace = null,
        SchemaTypeId? typeId = null,
        IReadOnlyList<Particle>? children = null)
    {
        Kind = kind;
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        ElementName = elementName;
        ElementNamespace = elementNamespace;
        TypeId = typeId;
        Children = children ?? Array.Empty<Particle>();
    }

    /// <summary>Compositor or element kind.</summary>
    public ParticleKind Kind { get; }

    /// <summary>Minimum occurrences.</summary>
    public decimal MinOccurs { get; }

    /// <summary>Maximum occurrences (<see cref="decimal.MaxValue"/> = unbounded).</summary>
    public decimal MaxOccurs { get; }

    /// <summary>Local element name when <see cref="Kind"/> is <see cref="ParticleKind.Element"/>.</summary>
    public string? ElementName { get; }

    /// <summary>Element namespace URI when applicable.</summary>
    public string? ElementNamespace { get; }

    /// <summary>Resolved type of the element particle.</summary>
    public SchemaTypeId? TypeId { get; }

    /// <summary>Child particles for compositors.</summary>
    public IReadOnlyList<Particle> Children { get; }

    /// <summary>Whether maxOccurs is unbounded.</summary>
    public bool IsUnbounded => MaxOccurs == decimal.MaxValue;

    /// <summary>Whether the particle may occur more than once.</summary>
    public bool IsCollection => MaxOccurs > 1;
}
