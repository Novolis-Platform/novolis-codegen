namespace Novolis.CodeGen.Xsd;

/// <summary>Context passed to <see cref="IXsdEmitHook"/>.</summary>
public sealed class XsdEmitContext
{
    /// <summary>Creates an emit context.</summary>
    public XsdEmitContext(EmitOptions options, IEmitProfile profile)
    {
        Options = options;
        Profile = profile;
    }

    /// <summary>Emit options used for this pass.</summary>
    public EmitOptions Options { get; }

    /// <summary>Profile that produced the files.</summary>
    public IEmitProfile Profile { get; }
}
