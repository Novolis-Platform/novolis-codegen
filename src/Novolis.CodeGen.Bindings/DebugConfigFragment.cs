namespace Novolis.CodeGen.Bindings;

/// <summary>Manifest fragment for debug capture configuration and native symbols.</summary>
/// <param name="Id">Fragment identifier.</param>
/// <param name="SchemaVersion">Manifest schema version.</param>
/// <param name="Description">Optional fragment description.</param>
/// <param name="NotifyAfterNativeCall">Hook invoked after each native call when debugging.</param>
/// <param name="FrameHubNotifyAfter">Frame hub notification hook name.</param>
/// <param name="CaptureEnvVar">Environment variable enabling capture.</param>
/// <param name="CapturePngFileType">PNG file type constant for capture.</param>
/// <param name="Symbols">Native symbol map.</param>
public sealed record DebugConfigFragment(
    string Id,
    int SchemaVersion,
    string? Description,
    string NotifyAfterNativeCall,
    string FrameHubNotifyAfter,
    string CaptureEnvVar,
    string CapturePngFileType,
    DebugSymbolMapSpec Symbols) : IManifestFragment
{
    /// <inheritdoc />
    public FragmentKind Kind => FragmentKind.DebugConfig;
}
