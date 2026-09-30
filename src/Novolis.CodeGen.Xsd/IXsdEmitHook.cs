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
