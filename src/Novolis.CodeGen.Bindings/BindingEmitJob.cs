using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>One emit job within a <see cref="BindingProject"/>.</summary>
/// <param name="Label">Short label for logs.</param>
/// <param name="FragmentKind">Expected manifest fragment kind.</param>
/// <param name="FragmentId">Manifest fragment identifier.</param>
/// <param name="Emitter">Emitter implementation.</param>
/// <param name="Target">Output target.</param>
/// <param name="Optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
/// <param name="FormatPolicy">Formatting applied after the raw source is emitted.</param>
/// <param name="Slice">Optional named slice within a multi-output fragment.</param>
public sealed record BindingEmitJob(
    string Label,
    FragmentKind FragmentKind,
    string FragmentId,
    IBindingEmitter Emitter,
    EmitTarget Target,
    bool Optional = false,
    BindingFormatPolicy FormatPolicy = BindingFormatPolicy.RoslynFormatter,
    string? Slice = null)
{
    /// <summary>Creates a <see cref="LibraryImportEmitter"/> job for an interop exports fragment.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Interop fragment identifier.</param>
    /// <param name="className">Generated partial class name.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="libraryConstantName">Generated constant naming the native library.</param>
    /// <param name="typeSummary">Optional generated type XML summary.</param>
    /// <param name="structSummary">Optional generated struct XML summary.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured library-import emit job.</returns>
    public static BindingEmitJob LibraryImport(
        string label,
        string fragmentId,
        string className,
        string relativePath,
        string namespaceName,
        string assemblyName,
        string? libraryConstantName = null,
        string? typeSummary = null,
        string? structSummary = null,
        bool optional = false) =>
        new(
            label,
            FragmentKind.InteropExports,
            fragmentId,
            new LibraryImportEmitter(),
            new EmitTarget(
                className,
                EmitStrategy.LibraryImport,
                relativePath,
                namespaceName,
                assemblyName,
                libraryConstantName,
                typeSummary,
                structSummary),
            optional);

    /// <summary>Creates a <see cref="DynamicExportsEmitter"/> job for a shim exports fragment.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Shim fragment identifier.</param>
    /// <param name="className">Generated partial class name.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured dynamic-export emit job.</returns>
    public static BindingEmitJob DynamicExports(
        string label,
        string fragmentId,
        string className,
        string relativePath,
        string namespaceName,
        string assemblyName,
        bool optional = false) =>
        new(
            label,
            FragmentKind.ShimExports,
            fragmentId,
            new DynamicExportsEmitter(),
            new EmitTarget(
                className,
                EmitStrategy.DynamicExports,
                relativePath,
                namespaceName,
                assemblyName),
            optional);

    /// <summary>Creates a <see cref="FacadeForwardEmitter"/> job for one façade type slice.</summary>
    /// <param name="label">Short label for logs.</param>
    /// <param name="fragmentId">Façade fragment identifier.</param>
    /// <param name="typeName">Façade type name and slice key.</param>
    /// <param name="relativePath">Output path relative to the repository root.</param>
    /// <param name="namespaceName">CLR namespace for the generated type.</param>
    /// <param name="assemblyName">Target assembly name.</param>
    /// <param name="facadeMethodImpl">Optional method implementation policy such as <c>AggressiveInlining</c>.</param>
    /// <param name="documentation">Optional consumer documentation resolver.</param>
    /// <param name="optional">When <see langword="true"/>, skipped unless optional jobs are included.</param>
    /// <returns>A configured façade-forward emit job.</returns>
    public static BindingEmitJob FacadeForward(
        string label,
        string fragmentId,
        string typeName,
        string relativePath,
        string namespaceName,
        string assemblyName,
        string? facadeMethodImpl = null,
        IFacadeDocumentationResolver? documentation = null,
        bool optional = false) =>
        new(
            label,
            FragmentKind.FacadeTypes,
            fragmentId,
            new FacadeForwardEmitter(documentation),
            new EmitTarget(
                typeName,
                EmitStrategy.FacadeForward,
                relativePath,
                namespaceName,
                assemblyName,
                FacadeMethodImpl: facadeMethodImpl),
            optional,
            BindingFormatPolicy.NormalizeWhitespace,
            typeName);
}
