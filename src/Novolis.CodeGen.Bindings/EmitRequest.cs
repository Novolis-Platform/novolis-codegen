using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Input to a single binding emitter invocation.</summary>
/// <param name="Fragment">Manifest fragment to emit from.</param>
/// <param name="ManifestSha256">Fingerprint embedded in generated headers.</param>
/// <param name="Target">Output target metadata.</param>
/// <param name="Context">Emit context (paths, environment, hints).</param>
public sealed record EmitRequest(
    IManifestFragment Fragment,
    string ManifestSha256,
    EmitTarget Target,
    BindingEmitContext Context);
