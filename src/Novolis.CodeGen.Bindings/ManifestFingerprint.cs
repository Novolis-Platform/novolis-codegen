using System.Security.Cryptography;
using System.Text;

namespace Novolis.CodeGen.Bindings;

public static class ManifestFingerprint
{
    public static string Sha256Hex(IManifestFragment fragment) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalText(fragment)))).ToLowerInvariant();

    public static string CanonicalText(IManifestFragment fragment) =>
        fragment switch
        {
            InteropExportsFragment interop => CanonicalInterop(interop),
            ShimExportsFragment shim => CanonicalShim(shim),
            DebugConfigFragment debug => CanonicalDebug(debug),
            FacadeTypesFragment facade => CanonicalFacade(facade),
            _ => throw new NotSupportedException($"Unsupported fragment type {fragment.GetType().Name}"),
        };

    private static string CanonicalInterop(InteropExportsFragment fragment)
    {
        var sb = new StringBuilder();
        sb.Append("interop|").Append(fragment.Id).Append('|').Append(fragment.SchemaVersion).Append('|')
            .Append(fragment.DllName).Append('|');
        AppendPolicy(sb, fragment.Policy);
        foreach (var st in fragment.Structs.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            sb.Append("|struct:").Append(st.Name);
            foreach (var field in st.Fields)
                sb.Append(':').Append(field.Name).Append('=').Append(field.ClrType);
        }

        foreach (var import in fragment.Imports.OrderBy(i => i.Name, StringComparer.Ordinal))
        {
            sb.Append("|import:").Append(import.Name).Append(':').Append(import.Template);
            if (import.Description is not null)
                sb.Append(':').Append(import.Description);
            if (import.SuppressGcTransition is { } suppress)
                sb.Append(":suppress=").Append(suppress);
        }

        return sb.ToString();
    }

    private static void AppendPolicy(StringBuilder sb, InteropPolicySpec policy)
    {
        sb.Append("policy:");
        foreach (var template in policy.SuppressGcTransitionByTemplate.OrderBy(t => t, StringComparer.Ordinal))
            sb.Append("suppressTemplate=").Append(template).Append(';');
        foreach (var name in policy.NeverSuppressGcTransition.OrderBy(n => n, StringComparer.Ordinal))
            sb.Append("neverSuppress=").Append(name).Append(';');
        if (policy.FacadeMethodImpl is not null)
            sb.Append("facadeImpl=").Append(policy.FacadeMethodImpl).Append(';');
        sb.Append("disableMarshalling=").Append(policy.UseDisableRuntimeMarshalling);
    }

    private static string CanonicalShim(ShimExportsFragment fragment)
    {
        var sb = new StringBuilder();
        sb.Append("shim|").Append(fragment.Id).Append('|').Append(fragment.SchemaVersion).Append('|')
            .Append(fragment.ModuleFileName).Append('|');
        foreach (var export in fragment.Exports.OrderBy(e => e.Export, StringComparer.Ordinal))
            sb.Append("|export:").Append(export.Export).Append(':').Append(export.Template);
        return sb.ToString();
    }

    private static string CanonicalDebug(DebugConfigFragment fragment)
    {
        var sb = new StringBuilder();
        sb.Append("debug|").Append(fragment.Id).Append('|').Append(fragment.SchemaVersion).Append('|')
            .Append(fragment.NotifyAfterNativeCall).Append('|').Append(fragment.FrameHubNotifyAfter).Append('|')
            .Append(fragment.CaptureEnvVar).Append('|').Append(fragment.CapturePngFileType).Append('|')
            .Append(fragment.Symbols.LoadImageFromScreen).Append('|')
            .Append(fragment.Symbols.ExportImageToMemory).Append('|')
            .Append(fragment.Symbols.UnloadImage).Append('|')
            .Append(fragment.Symbols.MemFree);
        return sb.ToString();
    }

    private static string CanonicalFacade(FacadeTypesFragment fragment)
    {
        var sb = new StringBuilder();
        sb.Append("facade|").Append(fragment.Id).Append('|');
        foreach (var type in fragment.Types.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            sb.Append("|type:").Append(type.Name).Append(':').Append(type.Namespace).Append(':').Append(type.Folder);
            if (type.TypeSummary is not null)
                sb.Append(':').Append(type.TypeSummary);
            foreach (var usingNs in type.Usings.OrderBy(u => u, StringComparer.Ordinal))
                sb.Append(":using=").Append(usingNs);
            foreach (var method in type.Methods.OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                sb.Append("|method:").Append(method.Name).Append(':').Append(method.Signature).Append(':')
                    .Append(method.Body);
                if (method.Summary is not null)
                    sb.Append(':').Append(method.Summary);
            }
        }

        return sb.ToString();
    }
}
