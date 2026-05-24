# Binding CodeGen Library — Specification (draft)

**Status:** draft  
**Reference consumer:** [novolis-raylib](https://github.com/Novolis-Platform/novolis-raylib)  
**Target repo:** [novolis-codegen](https://github.com/Novolis-Platform/novolis-codegen)

This document specifies a reusable C# library for **manifest-driven native binding generation**: fetch sources, verify manifests, emit interop/façade C#, apply Roslyn post-processing hooks, and pack native binaries. The design is **1:1 with what novolis-raylib does today**, expressed as composable fragments and explicit merge points so “Raylib + ImGui → one Bindings assembly” is ergonomic without losing the current emit semantics.

---

## 1. Goals

| Goal | Meaning |
|------|---------|
| **1:1 parity** | A `RaylibBindingProject` definition reproduces all eight manifests, eight emit targets, three hook phases, and pipeline step_06 output paths exactly. |
| **Library ergonomics** | Manifests authored in C# with IntelliSense; fluent merge for optional add-ons (Raygui); JSON as derived artifact for drift/agents. |
| **Roslyn as adaptation layer** | String emitters produce baseline syntax trees; hooks rewrite trees for docs, inlining, cross-cutting injection — not second emitters. |
| **Incremental pipeline** | Fingerprinted steps (`result.json`, input SHA256, skip-if-up-to-date) remain first-class; library supplies the kernel, consumers supply domain steps. |

---

## 2. Reference inventory (novolis-raylib as-is)

### 2.1 Pipeline steps

| Step | Id | Role |
|------|-----|------|
| 01 | `step_01_source` | Fetch raylib prebuilts, raygui header, raylib-cimgui into `artifacts/` (`versions.json`) |
| 02 | `step_02_native` | CMake build shims; copy DLLs to `artifacts/` |
| 03 | `step_03_verify_manifest` | Manifest imports vs fetched `raylib.h` |
| 04 | `step_04_enrich_docs` | Fill façade summaries from headers (writes manifests) |
| 05 | `step_05_verify_docs` | Fail on missing façade docs |
| 06 | `step_06_codegen` | Emit committed `*.g.cs` |
| 07 | `step_07_drift` | `git diff` on manifests + generated C# |
| 08 | `step_08_build` | Release build Bindings + Runtime |

Profiles: `maintainer`, `generate`, `ci-codegen`, `agent-verify`.

### 2.2 Manifest files → generated outputs (authoritative map)

| Manifest | Schema kind | Emitter | Phase | Output path | Partial class |
|----------|-------------|---------|-------|-------------|---------------|
| `raylib-exports.manifest.json` | Interop | `RaylibInteropEmitter` | `Interop` | `src/Novolis.Raylib.Bindings/Interop/Raylib6Native.g.cs` | `Raylib6Native` |
| `imgui-exports.manifest.json` | Shim exports | `ImguiInteropEmitter` | `ImGui` | `src/Novolis.Raylib.Bindings/Interop/ImguiShimExports.g.cs` | `ImguiShimExports` |
| `raylib-debug.manifest.json` | Debug config | `RaylibDebugHooksEmitter` | `Debug` | `src/Novolis.Raylib.Bindings/Interop/RaylibDebugFrameHooks.g.cs` | `RaylibDebugFrameHooks` |
| `facades.manifest.json` | Façade types | `FacadeEmitter` | `Facade` | `src/Novolis.Raylib.Runtime/{folder}/{Name}.g.cs` | per type |
| `hud.manifest.json` | Façade types | `FacadeEmitter` | `Facade` | `src/Novolis.Raylib.Runtime/Hud/Hud.g.cs` | `Hud` |
| `gui.manifest.json` | Façade types | `FacadeEmitter` | `Facade` | `src/Novolis.Raylib.Runtime/Gui/Gui.g.cs` | `Gui` |
| `raygui-exports.manifest.json` | Shim exports | `RayguiInteropEmitter` | `Raygui` | `src/Novolis.Raylib.Raygui/Interop/RayguiShimExports.g.cs` | `RayguiShimExports` |
| `raygui.manifest.json` | Façade types | `FacadeEmitter` | `Facade` | `src/Novolis.Raylib.Raygui/RayGui/RayGui.g.cs` | `RayGui` |

**Façade types from `facades.manifest.json` today:** `Graphics`, `Window`, `Input`, `World`, `Textures`, `Time`, `AudioDevice` (folders mirror names under `Runtime/`).

### 2.3 Emit strategies (do not flatten)

| Strategy | Used by | Generated shape |
|----------|---------|-----------------|
| `LibraryImport` | `raylib-exports` | `[LibraryImport]` partial static methods on `Raylib6Native`; blittable structs; `DisableRuntimeMarshalling` policy |
| `DynamicExports` | `imgui-exports`, `raygui-exports` | Function-pointer fields + `TryBindShim(nint module)` via `NativeLibrary.GetExport` |
| `DebugHooks` | `raylib-debug` | `#if DEBUG` events + capture helpers referencing symbols from interop manifest |
| `FacadeForward` | façade manifests | `public static partial class` with expression-bodied forwards to interop/shim |

Raylib + ImGui share **`Novolis.Raylib.Bindings`** and namespace **`Novolis.Raylib.Interop`** but remain **separate partial classes** — merge point orchestrates inclusion, not class merging.

### 2.4 Roslyn hooks (shipped today)

| Hook | Order | Phase | Behavior |
|------|-------|-------|----------|
| `AnnotateLibraryImportHook` | 10 | `Interop` | XML `/// <summary>` on `[LibraryImport]` methods from manifest `description` |
| `InjectEndDrawingNotifyHook` | 20 | `Facade` | Rewrites `Graphics.EndDrawing` body to notify debug hooks + frame capture hub |
| `FacadeInliningHook` | 30 | `Facade` | `[MethodImpl(AggressiveInlining)]` on expression-bodied façade methods when policy says so |

**Emit pipeline per file:**

```
string source = Emitter.Emit(manifestBytes, sha256)
CompilationUnitSyntax unit = CodegenFormatter.ParseGenerated(source)
foreach hook in hooks.Where(phase matches).OrderBy(Order):
    unit = hook.Transform(unit, context)
string file = Format(unit)   // interop: Formatter.Format; façade: NormalizeWhitespace
File.WriteAllText(outputPath, file)
```

Context carries: `RepoRoot`, `Phase`, `OutputPath`, `ManifestPath`, `ManifestSha256`, `RegenerateHint`, `ImportDescriptions`, `FacadeTypeName`, `FacadeMethodImpl`.

### 2.5 Native pack (Bindings csproj today)

| Binary | Source | Link name |
|--------|--------|-----------|
| `raylib.dll` | step_01 prebuilt | `raylib.dll` |
| `novolis_raylib_trace.dll` | step_02 / native out | `novolis_raylib_trace.dll` |
| `novolis_imgui.dll` | step_02 / native out | `novolis_imgui.dll` |
| `novolis_raygui.dll` | step_02 (optional `IncludeRaygui`) | `novolis_raygui.dll` |

Raygui interop/façade outputs live in **`Novolis.Raylib.Raygui`** add-on assembly, not Bindings — optional merge branch.

---

## 3. Library package layout (novolis-codegen)

```
Novolis.CodeGen.Pipeline          Step kernel: IPipelineStep, runner, skip, result.json
Novolis.CodeGen.Bindings          Manifest fragments, merge plan, serializers, emit orchestration
Novolis.CodeGen.Bindings.Roslyn   Parse/format, ICodegenHook, discovery, rewriter helpers
```

**Consumer (novolis-raylib):**

```
Novolis.Raylib.Manifests          C# manifest definitions + RaylibBindingProject (1:1)
Novolis.Raylib.CodeGen              Domain emitters (templates), verify, enrich, suggest
Novolis.Raylib.CodeGen.Hooks        Domain Roslyn hooks
Novolis.Raylib.Pipeline             Domain steps (source, native, drift, build)
```

Codegen projects stay **unpublished**; only `Novolis.CodeGen.*` packages ship on NuGet.

---

## 4. Manifest model (1:1 JSON schemas)

Each manifest file maps to a **typed fragment** with stable `Id`. Fragments serialize to the **exact JSON shape already committed** in raylib (round-trip required for drift).

### 4.1 Fragment kinds

```csharp
public enum FragmentKind
{
    InteropExports,     // raylib-exports.manifest.json
    ShimExports,        // imgui-exports, raygui-exports
    DebugConfig,        // raylib-debug.manifest.json
    FacadeTypes,        // facades, hud, gui, raygui manifests
    NativeArtifacts,    // logical ref to DLLs (not a JSON file today)
    SourceVersions,     // versions.json
}
```

### 4.2 Interop exports fragment

Mirrors `RaylibManifest` / `RaylibManifestModels`:

```csharp
public sealed record InteropExportsFragment(
    string Id,                          // "raylib6"
    int SchemaVersion,
    string? Header,                     // metadata only
    string? Description,
    string DllName,                     // "raylib"
    InteropPolicy Policy,
    IReadOnlyList<InteropStruct> Structs,
    IReadOnlyList<InteropImport> Imports);

public sealed record InteropPolicy(
    IReadOnlyList<string> SuppressGcTransitionByTemplate,
    IReadOnlyList<string> NeverSuppressGcTransition,
    string? FacadeMethodImpl,           // "AggressiveInlining"
    bool UseDisableRuntimeMarshalling);

public sealed record InteropImport(
    string Name,
    string Template,                    // e.g. "void_void" — see Template catalog §4.6
    string? Description,
    bool? SuppressGcTransition);
```

**C# authoring:**

```csharp
InteropExportsFragment.Create("raylib6")
    .SchemaVersion(2)
    .Header(Vendor.Raylib6.Header)
    .Dll("raylib")
    .Policy(p => p
        .SuppressGcTransitionByTemplate(
            Template.VoidVoid, Template.VoidColor, /* … full list from manifest … */)
        .NeverSuppress("MemFree", "LoadTexture")
        .FacadeMethodImpl("AggressiveInlining")
        .DisableRuntimeMarshalling())
    .Struct("Raylib6NativeImage", s => s
        .Field("Data", "nint")
        .Field("Width", "int")
        .Field("Height", "int")
        .Field("Mipmaps", "int")
        .Field("Format", "int"))
    .Import("BeginDrawing", Template.VoidVoid)
    .Import("EndDrawing", Template.VoidVoid)
    // bulk bootstrap: .ImportFromJson(path) or .SuggestFromHeader(header, filter)
    .Build();
```

### 4.3 Shim exports fragment

Mirrors `ImguiManifest` / `RayguiManifest`:

```csharp
public sealed record ShimExportsFragment(
    string Id,                          // "imgui" | "raygui"
    int SchemaVersion,
    string? Header,
    string? Description,
    string ModuleFileName,              // "novolis_imgui.dll" — used by NativePack, not in JSON today
    IReadOnlyList<ShimExport> Exports);

public sealed record ShimExport(string Export, string Template);
```

ImGui example (1:1 with `imgui-exports.manifest.json`):

```csharp
ShimExportsFragment.Create("imgui")
    .SchemaVersion(1)
    .Header(Vendor.Cimgui.Header)
    .Module("novolis_imgui")
    .Export("novolis_rlimgui_setup", Template.VoidInt)
    .Export("novolis_igBegin", Template.IntUtf8PtrIntInt)
    // …
    .Build();
```

### 4.4 Debug config fragment

Mirrors `raylib-debug.manifest.json`:

```csharp
public sealed record DebugConfigFragment(
    string Id,
    int SchemaVersion,
    string? Description,
    string NotifyAfterNativeCall,
    string FrameHubNotifyAfter,
    string CaptureEnvVar,
    string CapturePngFileType,
    DebugSymbolMap Symbols);

public sealed record DebugSymbolMap(
    string LoadImageFromScreen,
    string ExportImageToMemory,
    string UnloadImage,
    string MemFree);
```

Symbols reference **import names** from the interop fragment (`Raylib6Native.{name}`).

### 4.5 Façade types fragment

Mirrors `FacadesManifest` / `FacadeTypeDefinition`:

```csharp
public sealed record FacadeTypesFragment(
    string Id,                          // "facades" | "hud" | "gui" | "raygui"
    IReadOnlyList<FacadeType> Types);

public sealed record FacadeType(
    string Name,
    string Namespace,
    string Folder,
    string? TypeSummary,
    IReadOnlyList<string> Usings,
    IReadOnlyList<FacadeMethod> Methods);

public sealed record FacadeMethod(
    string Name,
    string Signature,                   // "void BeginDrawing()"
    string Body,                        // "Raylib6Native.BeginDrawing()"
    string? Summary);
```

Façade bodies are **opaque C# snippets** today — they cross-reference merged interop/shim types by name. Merge point `AllowCrossReference()` documents that dependency.

### 4.6 Template catalog

Templates are the contract between manifest and emitter switch arms. Library exposes them as constants that serialize to existing strings:

```csharp
public static class Template
{
    public static readonly TemplateId VoidVoid = "void_void";
    public static readonly TemplateId VoidColor = "void_color";
    public static readonly TemplateId IntUtf8PtrIntInt = "int_utf8_ptrint_int";
    // … one per case arm in RaylibInteropEmitter + ImguiInteropEmitter
}
```

Custom templates remain possible via implicit conversion from `string` for domain extensions.

### 4.7 JSON round-trip

```csharp
public interface IManifestFragment
{
    string Id { get; }
    FragmentKind Kind { get; }
    string FileName { get; }            // e.g. "raylib-exports.manifest.json"
    JsonDocument ToJson();              // canonical shape for drift
    byte[] ToUtf8Bytes() => …;
    string Sha256Hex() => …;
}

// Bootstrap from committed files during migration:
InteropExportsFragment.FromJson("codegen/pipeline/raylib6/raylib-exports.manifest.json");
```

**Authoring policy:** C# is source of truth; `SerializeManifests(dir)` writes JSON before drift check. During transition, hand-edited JSON can be imported once into C# definitions.

---

## 5. Merge points

Merge points are **named composition steps** in a `BindingProject`. They do not imply merging C# types unless `EmitStrategy` explicitly requests it.

### 5.1 Merge point catalog

| Merge point | Input fragments | Output | Raylib mapping |
|-------------|-----------------|--------|----------------|
| `Bindings` | `InteropExports`, `ShimExports`, `DebugConfig` | `*.g.cs` under Bindings `Interop/` | Raylib6Native + ImguiShimExports + RaylibDebugFrameHooks in one assembly |
| `Facades.Core` | `FacadeTypes` (`facades`, `hud`, `gui`) | `Runtime/**/*.g.cs` | Core runtime façades |
| `Facades.AddOn` | `FacadeTypes` (`raygui`) + `ShimExports` (`raygui`) | `Raygui/**/*.g.cs` | Optional add-on assembly |
| `NativePack` | `NativeArtifacts` | MSBuild item metadata / packaging | Bindings csproj copy rules |
| `Verify` | `InteropExports` + vendor header | pass/fail | step_03 |
| `EnrichDocs` | `FacadeTypes` + headers | updated façade JSON/C# | step_04 |

### 5.2 Bindings merge (Raylib + ImGui)

```csharp
BindingsMerge.Create()
    .TargetAssembly("Novolis.Raylib.Bindings")
    .TargetNamespace("Novolis.Raylib.Interop")
    .Include("raylib6", new EmitTarget(
        ClassName: "Raylib6Native",
        Strategy: EmitStrategy.LibraryImport,
        RelativePath: "Interop/Raylib6Native.g.cs",
        Phase: EmitPhase.Interop))
    .Include("imgui", new EmitTarget(
        ClassName: "ImguiShimExports",
        Strategy: EmitStrategy.DynamicExports,
        RelativePath: "Interop/ImguiShimExports.g.cs",
        Phase: EmitPhase.ImGui))
    .Include("raylib-debug", new EmitTarget(
        ClassName: "RaylibDebugFrameHooks",
        Strategy: EmitStrategy.DebugHooks,
        RelativePath: "Interop/RaylibDebugFrameHooks.g.cs",
        Phase: EmitPhase.Debug,
        DependsOn: ["raylib6"]))   // symbol names from interop imports
    .ConflictPolicy(ConflictPolicy.FailOnDuplicateTypeName)
    .Hooks(EmitPhase.Interop, EmitPhase.ImGui, EmitPhase.Debug);
```

Optional Raygui branch (separate assembly — matches today):

```csharp
.When(includeRaygui, plan => plan
    .BindingsAddOn("Novolis.Raylib.Raygui")
        .Include("raygui-shim", "RayguiShimExports", EmitStrategy.DynamicExports,
            "Interop/RayguiShimExports.g.cs", EmitPhase.Raygui));
```

### 5.3 Façades merge

```csharp
FacadesMerge.Create()
    .TargetRoot("src/Novolis.Raylib.Runtime")
    .Include("facades")           // Graphics, Window, Input, World, Textures, Time, AudioDevice
    .Include("hud")
    .Include("gui")                 // bodies reference ImguiShimExports
  .PolicyFrom("raylib6")            // FacadeMethodImpl → inlining hook + emitter
    .HeaderDocs(Vendor.Raylib6.Header, Vendor.Raygui.Header)
    .Hooks(EmitPhase.Facade);
```

### 5.4 `BindingSurface` sugar

Fluent composition without losing explicit merge semantics:

```csharp
public readonly struct BindingSurface
{
    public static BindingSurface Core { get; }      // raylib6 + debug
    public static BindingSurface ImGui { get; }     // imgui shim + gui façades

    public static BindingSurface operator +(BindingSurface a, BindingSurface b)
        => BindingSurface.Merge(a, b);

    public BindingProject IntoProject(string name) => …;
}

// Ergonomic:
var bindings = (BindingSurface.Core + BindingSurface.ImGui)
    .IntoProject("Novolis.Raylib")
    .WithNativePack(NativePack.Raylib6.WithImGui());
```

`operator+` adds merge edges to the plan graph; **no file I/O until** `project.Emit()` or pipeline step_06.

---

## 6. Full reference: `RaylibBindingProject` (1:1)

This is the target entrypoint replacing `RaylibCodegenPipeline.GenerateBindingsOnly()`:

```csharp
public static class RaylibBindingProject
{
    public static BindingProject Create(string repoRoot) =>
        BindingProject.Define("Novolis.Raylib", repoRoot)
            .Fragment(RaylibFragments.InteropExports)      // → raylib-exports.manifest.json
            .Fragment(RaylibFragments.ImGuiShim)           // → imgui-exports.manifest.json
            .Fragment(RaylibFragments.DebugConfig)         // → raylib-debug.manifest.json
            .Fragment(RaylibFragments.Facades)             // → facades.manifest.json
            .Fragment(RaylibFragments.Hud)                 // → hud.manifest.json
            .Fragment(RaylibFragments.Gui)                 // → gui.manifest.json
            .Fragment(RaylibFragments.RayguiShim)          // → raygui-exports.manifest.json
            .Fragment(RaylibFragments.RayguiFacade)          // → raygui.manifest.json
            .Fragment(RaylibFragments.NativePack)
            .Fragment(RaylibFragments.SourceVersions)      // → versions.json
            .Merge(plan => plan
                .Bindings(b => b
                    .Include(RaylibFragments.InteropExports, EmitTargets.Raylib6Native)
                    .Include(RaylibFragments.ImGuiShim, EmitTargets.ImguiShimExports)
                    .Include(RaylibFragments.DebugConfig, EmitTargets.DebugFrameHooks))
                .Facades(f => f
                    .Include(RaylibFragments.Facades, OutputRoot.Runtime)
                    .Include(RaylibFragments.Hud, OutputRoot.Runtime)
                    .Include(RaylibFragments.Gui, OutputRoot.Runtime)
                    .PolicyFrom(RaylibFragments.InteropExports)
                    .HeaderDocsFromSourceStep())
                .AddOn(a => a
                    .When(RaylibOptions.IncludeRaygui, branch => branch
                        .Bindings(EmitTargets.RayguiShimExports, OutputRoot.Raygui)
                        .Facades(RaylibFragments.RayguiFacade, OutputRoot.Raygui)
                        .Native(NativeArtifacts.RayguiDll)))
                .NativePack(NativeArtifacts.Raylib6Core))
            .SerializeDerivedManifests("codegen/pipeline/raylib6/")
            .RegisterHooksFromAssembly(typeof(AnnotateLibraryImportHook).Assembly)
            .Build();

    public static int Emit(string repoRoot)
    {
        var project = Create(repoRoot);
        return project.EmitAll();   // same files as step_06 CodegenOutputCatalog
    }
}
```

**Parity check:** `EmitAll()` output set must equal `CodegenOutputCatalog.AllGeneratedFiles(repoRoot)` byte-for-byte when fragments are bootstrapped from current JSON.

---

## 7. Roslyn adaptation layer (`Novolis.CodeGen.Bindings.Roslyn`)

Roslyn is the **adaptation surface** between dumb string emitters and final committed C#. The library should make hooks easy to write, test, and compose.

### 7.1 Core hook contract (generalized from `IRaylibCodegenHook`)

```csharp
public interface ICodegenHook<TPhase, in TContext>
    where TPhase : struct, Enum
{
    int Order { get; }
    TPhase Phase { get; }
    CompilationUnitSyntax Transform(CompilationUnitSyntax unit, TContext context);
}
```

Raylib consumer:

```csharp
public interface IRaylibCodegenHook : ICodegenHook<EmitPhase, RaylibCodegenContext> { }
```

### 7.2 Emit writer (centralizes today’s `WriteUnit`)

```csharp
public sealed class RoslynEmitWriter<TPhase, TContext>
    where TPhase : struct, Enum
{
    public void Write(
        string rawSource,
        TContext context,
        TPhase phase,
        string outputPath,
        IReadOnlyList<ICodegenHook<TPhase, TContext>> hooks,
        FormatPolicy format)
    {
        var unit = CSharpSyntaxTree.ParseText(rawSource).GetRoot() as CompilationUnitSyntax;
        foreach (var hook in hooks.Where(h => h.Phase.Equals(phase)).OrderBy(h => h.Order))
            unit = hook.Transform(unit!, context);
        var text = format.Apply(unit!);
        WriteFile(outputPath, text, context);
    }
}
```

`FormatPolicy`: `RoslynFormatter` (interop) vs `NormalizeWhitespace` (façades) — matches current behavior.

### 7.3 Rewriter helpers (library-provided)

Reduce boilerplate for common binding adaptations:

```csharp
public static class SyntaxRewriters
{
    public static CompilationUnitSyntax AddXmlDocToMethods(
        CompilationUnitSyntax unit,
        Func<MethodDeclarationSyntax, string?> summaryLookup);

    public static CompilationUnitSyntax AddAttributeToMethods(
        CompilationUnitSyntax unit,
        Func<MethodDeclarationSyntax, AttributeListSyntax?> attributeFactory);

    public static CompilationUnitSyntax RewriteMethodBody(
        CompilationUnitSyntax unit,
        string typeName,
        string methodName,
        Func<BlockSyntax?> bodyFactory);

    public static CompilationUnitSyntax EnsureUsing(
        CompilationUnitSyntax unit, string namespaceName);
}
```

`AnnotateLibraryImportHook` and `FacadeInliningHook` become thin wrappers over these helpers. `InjectEndDrawingNotifyHook` uses `RewriteMethodBody` for `Graphics.EndDrawing`.

### 7.4 Hook discovery

Generalize `HookDiscovery`:

```csharp
public static class HookDiscovery
{
    public static IReadOnlyList<ICodegenHook<TPhase, TContext>> Discover<TPhase, TContext>(
        params Assembly[] assemblies)
        where TPhase : struct, Enum;
}
```

Supports: hook assembly next to emitter, `[CodegenHook]` attribute for explicit registration, test injection via `BindingProject.RegisterHooks(...)`.

### 7.5 Testing hooks without emitters

```csharp
[Fact]
public void EndDrawing_hook_injects_notify()
{
    var source = """
        namespace Novolis.Raylib.Rendering;
        public static partial class Graphics {
            public static void EndDrawing() => Raylib6Native.EndDrawing();
        }
        """;
    var unit = Parse(source);
    var result = new InjectEndDrawingNotifyHook().Transform(unit, context);
    Assert.Contains("NotifyAfterEndDrawing", result.ToFullString());
}
```

Library ships `CodegenSyntaxAssert` helpers (contains/type/method lookup on trees).

### 7.6 Future Roslyn capabilities (explicitly out of v1 unless needed)

| Capability | Use case |
|------------|----------|
| **SyntaxFactory emitters** | Replace string `StringBuilder` emitters gradually |
| **IIncrementalGenerator** | Compile-time binding gen (different product; not replacing maintainer pipeline) |
| **SemanticModel** | Validate façade bodies reference existing interop methods at codegen time |
| **Roslyn analyzers** | CI guard: hand-edited `*.g.cs` detection (already partially via drift) |

---

## 8. Pipeline integration

### 8.1 Extract to `Novolis.CodeGen.Pipeline`

Move from `Novolis.Raylib.CodeGen.Abstractions` + `Novolis.Raylib.Pipeline` kernel:

- `IPipelineStep`, `PipelineContext`, `StepExecutionResult`, `StepResultDocument`
- `PipelineRunner`, `StepSkipEvaluator`, `StepResultWriter`, `StepFileFingerprint`
- `PipelineProfiles` pattern (consumer defines profiles)

**De-raylib:** `PipelinePaths` becomes injectable `IPipelineLayout`:

```csharp
public interface IPipelineLayout
{
    string RepoRoot { get; }
    string StepsRoot { get; }
    string StepDir(string stepId);
    string ManifestDir { get; }
}
```

Raylib implements `RaylibPipelineLayout` with `codegen/pipeline/raylib6/`.

### 8.2 step_06 wiring

```csharp
// Before:
var pipeline = new RaylibCodegenPipeline(context.RepoRoot);
pipeline.GenerateBindingsOnly(context.Log);

// After:
RaylibBindingProject.Emit(context.RepoRoot);
```

Input hashing for step_06: all `*.manifest.json` in manifest dir **or** SHA256 of C# fragment source files if JSON is derived-only.

---

## 9. Ergonomics checklist

What makes the library comfortable day-to-day:

| Feature | Benefit |
|---------|---------|
| `Template.*` constants | No typos in template strings; rename-safe |
| `.Import("Foo", Template.VoidVoid)` | Same shape as JSON row, better IDE support |
| `.SuggestFromHeader(path)` | Replaces `suggest-raylib` CLI for exploration |
| `.ImportFromJson(path)` | One-time bootstrap from committed manifests |
| `BindingSurface` `+` | Combine Core + GUI without copy-pasting merge blocks |
| `.When(condition, …)` | Raygui optional path matches `IncludeRaygui` MSBuild flag |
| `project.EmitDryRun()` | Print planned outputs + SHA256 without writing |
| `project.DiffAgainstRepo()` | Pre-commit drift preview (step_07 helper) |
| Hook unit tests via parsed snippets | Fast feedback without full pipeline |

---

## 10. Migration plan

| Phase | Work | Risk |
|-------|------|------|
| **0** | Document parity (this spec) | — |
| **1** | Extract `Novolis.CodeGen.Pipeline` + `Novolis.CodeGen.Bindings.Roslyn`; raylib references packages | Low |
| **2** | Add fragment records + JSON round-trip tests against all 8 manifest files | Low |
| **3** | Introduce `BindingProject` executor wrapping **existing** emitters unchanged | Low |
| **4** | `RaylibBindingProject` + switch step_06; parity test: emit diff empty | Medium |
| **5** | C# manifest authoring in `Novolis.Raylib.Manifests`; JSON becomes derived | Medium |
| **6** | Optional: SyntaxFactory emitters for one template group | Low priority |

**Do not migrate yet:** flattening interop classes, changing manifest JSON schema, removing SHA256 headers from `*.g.cs`.

---

## 11. Open questions

1. **Façade authoring in C#:** Full method bodies in code vs keep JSON/generated for large façades with enrich step — hybrid (`Partial` classes per façade type file)?
2. **Semantic validation:** Should `BindingProject.Build()` fail if `Gui.Setup` body references unknown symbol (needs parse + symbol table of merged interop)?
3. **Package granularity:** Ship `Novolis.CodeGen.Bindings` as one package or split Pipeline / Roslyn for lighter dependents?
4. **Second consumer:** Which repo validates generics first — `novolis-rendering` (Silk) or stay raylib-only until parity tests pass?

---

## 12. Summary

- **Today:** eight JSON manifests, four emit strategies, three hook phases, one Bindings assembly combining Raylib + ImGui interop, optional Raygui add-on.
- **Library:** fragment types + merge points express that graph in C#; JSON derived for agents/drift; pipeline kernel and Roslyn hook host shared via `novolis-codegen`.
- **Roslyn role:** parse → ordered hook rewrites → format; helpers lower the cost of adaptations like doc injection, inlining, and cross-cutting notify injection.
- **Success criterion:** `RaylibBindingProject.Emit(repoRoot)` is byte-identical to current `step_06_codegen` on a clean tree bootstrapped from existing manifests.
