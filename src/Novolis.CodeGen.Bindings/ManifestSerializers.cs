using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Bindings;

internal static class JsonDefaults
{
    internal static readonly JsonSerializerOptions Read = new() { PropertyNameCaseInsensitive = true };

    internal static readonly JsonSerializerOptions Write = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

public static class InteropExportsSerializer
{
    public static InteropExportsFragment LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return LoadFromUtf8Bytes(bytes, Path.GetFileName(path));
    }

    public static InteropExportsFragment LoadFromUtf8Bytes(ReadOnlySpan<byte> utf8, string fileName = "raylib-exports.manifest.json")
    {
        var doc = JsonSerializer.Deserialize<InteropWireDocument>(utf8, JsonDefaults.Read)
                  ?? throw new InvalidOperationException($"Failed to parse {fileName}");

        var policy = doc.InteropPolicy ?? new InteropPolicyWire();
        return new InteropExportsFragment(
            Id: "raylib6",
            SchemaVersion: doc.SchemaVersion ?? 1,
            Header: doc.Header,
            Description: doc.Description,
            DllName: doc.DllName ?? "raylib",
            Policy: new InteropPolicySpec(
                policy.SuppressGcTransitionByTemplate ?? [],
                policy.NeverSuppressGcTransition ?? [],
                policy.FacadeMethodImpl,
                policy.UseDisableRuntimeMarshalling ?? false),
            Structs: (doc.Structs ?? []).Select(s => new InteropStructSpec(
                s.Name ?? "",
                (s.Fields ?? []).Select(f => new InteropFieldSpec(f.Name ?? "", f.ClrType ?? "")).ToList())).ToList(),
            Imports: (doc.Imports ?? []).Select(i => new InteropImportSpec(
                i.Name ?? "",
                i.Template ?? "",
                i.Description,
                i.SuppressGcTransition)).ToList());
    }

    public static byte[] SerializeToUtf8Bytes(InteropExportsFragment fragment)
    {
        var wire = new InteropWireDocument
        {
            SchemaVersion = fragment.SchemaVersion,
            Header = fragment.Header,
            Description = fragment.Description,
            DllName = fragment.DllName,
            InteropPolicy = new InteropPolicyWire
            {
                SuppressGcTransitionByTemplate = fragment.Policy.SuppressGcTransitionByTemplate.ToList(),
                NeverSuppressGcTransition = fragment.Policy.NeverSuppressGcTransition.ToList(),
                FacadeMethodImpl = fragment.Policy.FacadeMethodImpl,
                UseDisableRuntimeMarshalling = fragment.Policy.UseDisableRuntimeMarshalling,
            },
            Structs = fragment.Structs.Select(s => new InteropStructWire
            {
                Name = s.Name,
                Fields = s.Fields.Select(f => new InteropFieldWire { Name = f.Name, ClrType = f.ClrType }).ToList(),
            }).ToList(),
            Imports = fragment.Imports.Select(i => new InteropImportWire
            {
                Name = i.Name,
                Template = i.Template,
                Description = i.Description,
                SuppressGcTransition = i.SuppressGcTransition,
            }).ToList(),
        };

        return JsonSerializer.SerializeToUtf8Bytes(wire, JsonDefaults.Write);
    }

    private sealed class InteropWireDocument
    {
        public int? SchemaVersion { get; set; }
        public string? Header { get; set; }
        public string? Description { get; set; }
        public string? DllName { get; set; }
        public InteropPolicyWire? InteropPolicy { get; set; }
        public List<InteropStructWire>? Structs { get; set; }
        public List<InteropImportWire>? Imports { get; set; }
    }

    private sealed class InteropPolicyWire
    {
        public List<string>? SuppressGcTransitionByTemplate { get; set; }
        public List<string>? NeverSuppressGcTransition { get; set; }
        public string? FacadeMethodImpl { get; set; }
        public bool? UseDisableRuntimeMarshalling { get; set; }
    }

    private sealed class InteropStructWire
    {
        public string? Name { get; set; }
        public List<InteropFieldWire>? Fields { get; set; }
    }

    private sealed class InteropFieldWire
    {
        public string? Name { get; set; }
        public string? ClrType { get; set; }
    }

    private sealed class InteropImportWire
    {
        public string? Name { get; set; }
        public string? Template { get; set; }
        public string? Description { get; set; }
        public bool? SuppressGcTransition { get; set; }
    }
}

public static class ShimExportsSerializer
{
    public static ShimExportsFragment LoadFromFile(string path, string id, string moduleFileName)
    {
        var bytes = File.ReadAllBytes(path);
        return LoadFromUtf8Bytes(bytes, id, moduleFileName, Path.GetFileName(path));
    }

    public static ShimExportsFragment LoadFromUtf8Bytes(
        ReadOnlySpan<byte> utf8,
        string id,
        string moduleFileName,
        string fileName = "imgui-exports.manifest.json")
    {
        var doc = JsonSerializer.Deserialize<ShimWireDocument>(utf8, JsonDefaults.Read)
                  ?? throw new InvalidOperationException($"Failed to parse {fileName}");

        return new ShimExportsFragment(
            id,
            doc.SchemaVersion ?? 1,
            doc.Header,
            doc.Description,
            moduleFileName,
            (doc.Functions ?? []).Select(f => new ShimExportSpec(f.Export ?? "", f.Template ?? "")).ToList());
    }

    public static byte[] SerializeToUtf8Bytes(ShimExportsFragment fragment)
    {
        var wire = new ShimWireDocument
        {
            SchemaVersion = fragment.SchemaVersion,
            Header = fragment.Header,
            Description = fragment.Description,
            Functions = fragment.Exports.Select(e => new ShimFunctionWire { Export = e.Export, Template = e.Template }).ToList(),
        };

        return JsonSerializer.SerializeToUtf8Bytes(wire, JsonDefaults.Write);
    }

    private sealed class ShimWireDocument
    {
        public int? SchemaVersion { get; set; }
        public string? Header { get; set; }
        public string? Description { get; set; }
        public List<ShimFunctionWire>? Functions { get; set; }
    }

    private sealed class ShimFunctionWire
    {
        public string? Export { get; set; }
        public string? Template { get; set; }
    }
}

public static class DebugConfigSerializer
{
    public static DebugConfigFragment LoadFromFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return LoadFromUtf8Bytes(bytes, Path.GetFileName(path));
    }

    public static DebugConfigFragment LoadFromUtf8Bytes(ReadOnlySpan<byte> utf8, string fileName = "raylib-debug.manifest.json")
    {
        var doc = JsonSerializer.Deserialize<DebugWireDocument>(utf8, JsonDefaults.Read)
                  ?? throw new InvalidOperationException($"Failed to parse {fileName}");

        var symbols = doc.Symbols ?? throw new InvalidOperationException("Missing symbols.");
        return new DebugConfigFragment(
            "raylib-debug",
            doc.SchemaVersion ?? 1,
            doc.Description,
            doc.NotifyAfterNativeCall ?? "EndDrawing",
            doc.FrameHubNotifyAfter ?? "EndDrawing",
            doc.CaptureEnvVar ?? "NOVOLIS_RAYLIB_DEBUG_CAPTURE",
            doc.CapturePngFileType ?? ".png",
            new DebugSymbolMapSpec(
                symbols.LoadImageFromScreen ?? "",
                symbols.ExportImageToMemory ?? "",
                symbols.UnloadImage ?? "",
                symbols.MemFree ?? ""));
    }

    public static byte[] SerializeToUtf8Bytes(DebugConfigFragment fragment)
    {
        var wire = new DebugWireDocument
        {
            SchemaVersion = fragment.SchemaVersion,
            Description = fragment.Description,
            NotifyAfterNativeCall = fragment.NotifyAfterNativeCall,
            FrameHubNotifyAfter = fragment.FrameHubNotifyAfter,
            CaptureEnvVar = fragment.CaptureEnvVar,
            CapturePngFileType = fragment.CapturePngFileType,
            Symbols = new DebugSymbolsWire
            {
                LoadImageFromScreen = fragment.Symbols.LoadImageFromScreen,
                ExportImageToMemory = fragment.Symbols.ExportImageToMemory,
                UnloadImage = fragment.Symbols.UnloadImage,
                MemFree = fragment.Symbols.MemFree,
            },
        };

        return JsonSerializer.SerializeToUtf8Bytes(wire, JsonDefaults.Write);
    }

    private sealed class DebugWireDocument
    {
        public int? SchemaVersion { get; set; }
        public string? Description { get; set; }
        public string? NotifyAfterNativeCall { get; set; }
        public string? FrameHubNotifyAfter { get; set; }
        public string? CaptureEnvVar { get; set; }
        public string? CapturePngFileType { get; set; }
        public DebugSymbolsWire? Symbols { get; set; }
    }

    private sealed class DebugSymbolsWire
    {
        public string? LoadImageFromScreen { get; set; }
        public string? ExportImageToMemory { get; set; }
        public string? UnloadImage { get; set; }
        public string? MemFree { get; set; }
    }
}

public static class FacadeTypesSerializer
{
    public static FacadeTypesFragment LoadFromFile(string path, string id)
    {
        var bytes = File.ReadAllBytes(path);
        return LoadFromUtf8Bytes(bytes, id, Path.GetFileName(path));
    }

    public static FacadeTypesFragment LoadFromUtf8Bytes(ReadOnlySpan<byte> utf8, string id, string fileName)
    {
        var doc = JsonSerializer.Deserialize<FacadeWireDocument>(utf8, JsonDefaults.Read)
                  ?? throw new InvalidOperationException($"Failed to parse {fileName}");

        return new FacadeTypesFragment(
            id,
            (doc.Types ?? []).Select(t => new FacadeTypeSpec(
                t.Name ?? "",
                t.Namespace ?? "",
                t.Folder ?? "",
                t.TypeSummary,
                t.Usings ?? [],
                (t.Methods ?? []).Select(m => new FacadeMethodSpec(
                    m.Name ?? "",
                    m.Signature ?? "",
                    m.Body ?? "",
                    m.Summary)).ToList())).ToList());
    }

    public static byte[] SerializeToUtf8Bytes(FacadeTypesFragment fragment)
    {
        var wire = new FacadeWireDocument
        {
            Types = fragment.Types.Select(t => new FacadeTypeWire
            {
                Name = t.Name,
                Namespace = t.Namespace,
                Folder = t.Folder,
                TypeSummary = t.TypeSummary,
                Usings = t.Usings.ToList(),
                Methods = t.Methods.Select(m => new FacadeMethodWire
                {
                    Name = m.Name,
                    Signature = m.Signature,
                    Body = m.Body,
                    Summary = m.Summary,
                }).ToList(),
            }).ToList(),
        };

        return JsonSerializer.SerializeToUtf8Bytes(wire, JsonDefaults.Write);
    }

    public static bool SemanticEquals(FacadeTypesFragment a, FacadeTypesFragment b) =>
        a.Id == b.Id && a.Types.Count == b.Types.Count &&
        a.Types.Zip(b.Types).All(pair => pair.First.Name == pair.Second.Name &&
                                         pair.First.Methods.Count == pair.Second.Methods.Count);

    private sealed class FacadeWireDocument
    {
        public List<FacadeTypeWire>? Types { get; set; }
    }

    private sealed class FacadeTypeWire
    {
        public string? Name { get; set; }
        public string? Namespace { get; set; }
        public string? Folder { get; set; }
        public string? TypeSummary { get; set; }
        public List<string>? Usings { get; set; }
        public List<FacadeMethodWire>? Methods { get; set; }
    }

    private sealed class FacadeMethodWire
    {
        public string? Name { get; set; }
        public string? Signature { get; set; }
        public string? Body { get; set; }
        public string? Summary { get; set; }
    }
}

public static class ManifestSemanticEquality
{
    public static bool InteropEquals(InteropExportsFragment a, InteropExportsFragment b) =>
        a.DllName == b.DllName &&
        a.Imports.Count == b.Imports.Count &&
        a.Imports.OrderBy(i => i.Name).SequenceEqual(b.Imports.OrderBy(i => i.Name));

    public static bool ShimEquals(ShimExportsFragment a, ShimExportsFragment b) =>
        a.Exports.Count == b.Exports.Count &&
        a.Exports.OrderBy(e => e.Export).SequenceEqual(b.Exports.OrderBy(e => e.Export));

    public static bool DebugEquals(DebugConfigFragment a, DebugConfigFragment b) =>
        a.NotifyAfterNativeCall == b.NotifyAfterNativeCall &&
        a.FrameHubNotifyAfter == b.FrameHubNotifyAfter &&
        a.Symbols.LoadImageFromScreen == b.Symbols.LoadImageFromScreen;
}
