namespace Novolis.CodeGen.Xsd;

/// <summary>
/// Post-emit hook to reshape generated compilation units (records vs classes, extra bases, renames, …).
/// Applied by <see cref="XsdCodegen"/> after a profile emits.
/// </summary>
public interface IXsdEmitHook
{
    /// <summary>Lower runs first.</summary>
    int Order { get; }

    /// <summary>Transform one emitted file; return the same or a replacement.</summary>
    EmittedFile Transform(EmittedFile file, XsdEmitContext context);
}

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
