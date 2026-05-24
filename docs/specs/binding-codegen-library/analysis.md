# Binding CodeGen Library — Stress Test & Gap Analysis

**Status:** working analysis (feeds spec revisions; `initial-idea.md` stays read-only until plugs are agreed)  
**Reference:** [initial-idea.md](./initial-idea.md) vs [novolis-raylib](https://github.com/Novolis-Platform/novolis-raylib) codegen as of the current tree  
**Purpose:** Poke holes in the draft spec, map each to evidence in the reference implementation, and propose concrete plugs.

---

## Executive summary

The draft spec correctly identifies fragments, merge points, and Roslyn hooks as the right abstraction shape. Under stress against the real raylib repo, **the biggest gaps are not in merge syntax** — they are in **layers the spec never names**:

1. A **three-tier GUI stack** (shim exports → hand-written controls/host → generated façade) that breaks the “façade bodies call interop directly” story.
2. **Emitter-owned extras** (embedded structs, namespaces, template vocabularies) that are not manifest data.
3. **Dual truth for docs** (manifest JSON + `FacadeDocResolver` hardcoded defaults + header enrich) conflicting with “C# manifests are source of truth.”
4. **Three codegen entry points** (Pipeline, MSBuild `GenerateRaylibBindings`, legacy `CodeGen` CLI) not unified in the spec.
5. **Byte-for-byte parity** as success criterion vs tests and formatters that only guarantee subset checks today.

**Recommendation before implementation:** extend the spec with a **Binding stack model** (generated / hand companion / hook-adapted), an **`IEmitStrategy` plugin surface**, and explicit **authoring vs derived artifact** rules for enrich/drift. Defer C# manifest authoring (spec phase 5) until JSON round-trip and executor parity pass.

---

## Severity legend

| Level | Meaning |
|-------|---------|
| **P0** | Spec claim contradicts code or would cause wrong design if built as written |
| **P1** | Important missing piece; library works for raylib only with ad-hoc exceptions |
| **P2** | Inaccuracy, underspec, or future pain; not blocking parity work |

---

## P0 — Critical holes

### P0-1: Three-tier GUI stack is absent from the model

**Spec says:** Façade bodies cross-reference `ImguiShimExports`; merge point `AllowCrossReference()` covers Gui → interop.

**Code says:** Generated `Gui` forwards to **hand-written** `GuiControls`, which calls `ImguiShimExports` via `ImguiShimHost`:

```
ImguiShimExports.g.cs  (Bindings, generated)
    ↑ TryBindShim via
ImguiShimHost.cs       (Runtime, hand-written, public)
    ↑ EnsureInitialized + UTF-8 marshalling via
GuiControls.cs         (Runtime, hand-written, public)
    ↑ expression body via
Gui.g.cs               (Runtime, generated façade)
```

Same pattern for Raygui: `RayguiShimExports` → `RayGuiControls` → `RayGui.g.cs`.

**Why it breaks:** A consumer defining `Gui` manifest bodies as `ImguiShimExports.*_ptr(...)` would diverge from production. Merge semantics that only validate interop symbols will miss the real contract (`GuiControls.*`).

**Plug:**

- Add fragment kind **`CompanionSurface`** (or `ControlsLayer`) — hand-written, not emitted, but **declared in the binding project** so merge validates façade → controls → shim edges.
- Document stack per strategy:

| Tier | Raylib core | ImGui | Raygui |
|------|-------------|-------|--------|
| L0 native | `raylib.dll` | `novolis_imgui.dll` | `novolis_raygui.dll` |
| L1 generated interop | `Raylib6Native` | `ImguiShimExports` | `RayguiShimExports` |
| L2 hand companion | marshalling types, `Utf8StringMarshaller` | `ImguiShimHost`, `GuiControls` | `RayGuiControls` |
| L3 generated façade | `Graphics`, … | `Gui` | `RayGui` |

- Façade merge `AllowCrossReference()` should mean **“resolve against declared companion + interop symbol table”**, not “direct shim pointers.”

---

### P0-2: Shim JSON shape ≠ unified `ShimExportsFragment` on disk

**Spec says:** One `ShimExportsFragment` record with `Exports[]`.

**Code says:**

| File | Root array key | Row keys |
|------|----------------|----------|
| `imgui-exports.manifest.json` | `functions` | `export`, `template` |
| `raygui-exports.manifest.json` | `functions` | `export`, `template` |
| `raylib-exports.manifest.json` | `imports` | `name`, `template`, `description`, … |

ImGui/Raygui deserializers use **different internal types** (`ImguiManifest` vs `RayguiManifest`) despite identical JSON.

**Why it breaks:** “Exact JSON round-trip” requires **per-file serializers**, not one generic shim fragment serializer. A single C# record can still be the authoring model if `FileName` + `ToJson()` dispatch to the correct wire format.

**Plug:**

- `IManifestFragment` exposes `WireFormat` or `Serialize()` returns file-specific JSON.
- Shared **authoring** model; **split serializers** (`InteropExportsSerializer`, `ShimExportsSerializer`).
- Do not claim one JSON schema for all shim manifests unless raylib migrates filenames/keys (out of scope).

---

### P0-3: ImGui and Raygui template vocabularies are disjoint

**Spec says:** Global `Template.*` catalog covering “one per case arm in RaylibInteropEmitter + ImguiInteropEmitter.”

**Code says:** Three independent `switch` tables:

- **Raylib interop:** ~30 templates (`void_camera3d`, `nint_image_string_utf8_out_int`, …)
- **ImGui shim:** 9 templates (`int_utf8_ptrfloat_float_float`, …)
- **Raygui shim:** 10 templates (`int_rect_utf8`, `int_rect_utf8_utf8_ptrfloat_float_float`, …) **plus** emitter-hardcoded `RayguiRectangle` struct

**Why it breaks:** A single flat `Template` class implies false sharing; using wrong template compiles in C# authoring but throws at emit time.

**Plug:**

- Namespace templates: `InteropTemplate.*`, `ImGuiTemplate.*`, `RayguiTemplate.*` (or generic `TemplateScope` on builder).
- **`EmitStrategy` carries allowed template set** validated at `Build()`.
- **`EmbeddedTypes`** on shim strategy for Raygui’s `RayguiRectangle` (not in manifest today).

---

### P0-4: `InjectEndDrawingNotifyHook` bypasses fragment context

**Spec says:** Hooks attach to merge outputs; debug config is a `DebugConfigFragment`.

**Code says:** Hook **re-reads** `raylib-debug.manifest.json` from disk via `PipelinePaths.PipelineRaylibDir` inside `Transform()` — not from `RaylibCodegenContext`.

**Why it breaks:** When C# becomes source of truth and JSON is derived-only, hook behavior can drift from the fragment used to emit debug hooks unless the file was serialized first. Hook order vs emit order becomes fragile.

**Plug:**

- Extend context: `DebugConfigFragment? DebugConfig` populated by executor from merge graph.
- Hooks must not read manifest paths directly; **`BindingEmitContext` is the only config channel**.
- Cross-assembly reference: hook injects `Novolis.Raylib.Runtime.Presentation.RaylibPresentationHooks` — document as **`HookCapability.CrossAssembly`** with explicit allowed type list in binding project.

---

### P0-5: Enrich-docs (step_04) contradicts “C# manifests authoritative”

**Spec says:** Phase 5 — C# authoritative; JSON derived; `SerializeManifests` before drift.

**Code says:** `FacadeDocEnricher` **mutates** `facades.manifest.json`, `hud.manifest.json`, `gui.manifest.json` on disk from header comments. `FacadeDocResolver` also supplies hardcoded defaults when manifest fields are empty (verify passes without manifest text).

**Why it breaks:** Two authoring surfaces (C# + enrich writing JSON + resolver defaults). Migrating to C# first without relocating enrich logic loses automation agents rely on.

**Plug:**

- Split doc resolution into explicit phases in spec:
  1. **Manifest text** (authoritative summaries when present)
  2. **Header enrich** (optional transform, writes derived JSON or updates in-memory fragment)
  3. **Resolver defaults** (fallback dictionary — today in `FacadeDocResolver`, should move to `RaylibFragments` or JSON `defaults.json`)
- **`EnrichDocs` merge point** must declare whether it writes C#, JSON, or only affects emit-time docs without persisting.
- Until decided: **keep JSON authoritative for façades** in v1; C# only for interop/shim/debug first.

---

### P0-6: Byte-for-byte parity is stronger than existing guarantees

**Spec says:** `EmitAll()` byte-identical to current step_06.

**Code says:** Tests check `ManifestSha256` line and substring presence (`RaylibCodegenPipelineTests`), not full file hash. Formatting differs by phase:

| Phase | Formatter |
|-------|-----------|
| `Facade` | `NormalizeWhitespace` only |
| `Interop`, `ImGui`, `Raygui`, `Debug` | Roslyn `Formatter.Format` |

Roslyn formatter version bumps can change whitespace without semantic change.

**Plug:**

- Parity tiers:
  - **T0:** Same file set and `ManifestSha256` headers (match today’s tests)
  - **T1:** Normalized AST equivalence (parse both, compare syntax trees ignoring trivia)
  - **T2:** Byte-identical (optional, pin Roslyn version in test harness)
- Spec success criterion → **T1**, not T2, unless CI pins formatter.

---

## P1 — Significant gaps

### P1-1: Hand-written interop companions not in merge model

**Evidence:** `Bindings/Interop/` contains non-generated: `Utf8StringMarshaller.cs`, `RaylibInteropMarshaling.cs`, `RaylibColor.cs`, `RaylibDebugCaptureGate.cs`.

**Gap:** Merge point `Bindings` only lists `*.g.cs`. Emitters assume `[MarshalUsing(typeof(Utf8StringMarshaller))]` when policy enables `DisableRuntimeMarshalling`.

**Plug:** `BindingProject` declares **`RequiredCompanions`** — paths to hand files that must exist when a fragment policy is active. Codegen does not emit them; pack/build validates presence.

---

### P1-2: NativePack merge is aspirational — csproj is hand-maintained

**Evidence:** `Novolis.Raylib.Bindings.csproj` has conditional `None` copy rules, fallbacks to `codegen/native/.../out`, platform gates, `IncludeRaygui`.

**Gap:** Spec shows fluent `.NativePack().CopyToOutput(...)` but no generator exists.

**Plug:**

- v1: **NativePack = documentation + validation only** (check files exist at expected paths).
- v2: optional **MSBuild fragment emitter** (`Novolis.Raylib.Bindings.csproj` snippet generation) behind explicit flag.
- Wire `RaylibOptions.IncludeRaygui` to **same flag** as MSBuild `$(IncludeRaygui)` and pipeline `.When()`.

---

### P1-3: Raygui optional path is tri-state in code, binary in spec

**Evidence:**

- `EmitRayguiInterop` skips if manifest missing
- `CodegenOutputCatalog` conditionally includes raygui outputs if files exist
- Bindings csproj excludes raygui from Bindings assembly; Raygui is separate package
- `IncludeRaygui` MSBuild property

**Gap:** Spec `.When(includeRaygui)` does not define who sets the flag (env, MSBuild, CLI).

**Plug:** `BindingProjectOptions` with explicit precedence: CLI `--with-raygui` > MSBuild > default false. Emit plan lists **skipped targets** in dry-run output.

---

### P1-4: Namespace / assembly layout is inconsistent for Raygui shims

**Evidence:** `RayguiShimExports.g.cs` lives in **`Novolis.Raylib.Raygui`** assembly but declares `namespace Novolis.Raylib.Interop`.

**Gap:** Spec maps output to assembly by folder; namespace is emitter-hardcoded. Consumers reading “Bindings merge = one assembly” may miss that shim **file location** and **namespace** decouple.

**Plug:** `EmitTarget` fields: `Assembly`, `RelativePath`, `Namespace`, `ClassName` — all explicit. Raylib reference project sets Raygui shim to `Assembly=Raygui`, `Namespace=Novolis.Raylib.Interop`.

---

### P1-5: Façade folder layout spec inaccuracy

**Spec says:** “folders mirror names under Runtime/”

**Code says:** `World` and `Textures` use `"folder": "Rendering"` (shared with `Graphics`). Namespaces vary: `Novolis.Raylib.Rendering`, `Novolis.Raylib`, etc.

**Plug:** `FacadeType` keeps independent `Folder` and `Namespace` (spec already has fields — remove misleading prose). Add validation: folder is physical path only.

---

### P1-6: No `IEmitter` plugin boundary

**Spec puts:** “emit orchestration” in `Novolis.CodeGen.Bindings` but emitters stay in `Novolis.Raylib.CodeGen`.

**Gap:** Executor calls `RaylibInteropEmitter.Emit(...)` by name — library cannot host orchestration without a plugin contract.

**Plug:**

```csharp
public interface IBindingEmitter
{
    EmitStrategy Strategy { get; }
    string Emit(EmitRequest request);  // manifest bytes, sha256, layout hints
}

public sealed record EmitRequest(
    IManifestFragment Fragment,
    EmitTarget Target,
    BindingEmitContext Context);
```

Register emitters in `BindingProject`: `.UseEmitter<RaylibInteropEmitter>(EmitStrategy.LibraryImport)`.

---

### P1-7: Three entry points not unified

| Entry | Trigger |
|-------|---------|
| `dotnet run --project Novolis.Raylib.Pipeline -- run generate` | Maintainer / CI profile |
| `dotnet run --project Novolis.Raylib.CodeGen -- generate` | Legacy CLI (still in emitted file headers) |
| MSBuild `GenerateRaylibBindings` | Before `CoreCompile` on Bindings |

**Gap:** Spec only mentions Pipeline step_06. MSBuild inputs include codegen **source** `.cs` files — changing emitter triggers rebuild.

**Plug:** Single **`IBindingCodegenHost.Emit(GenerateProfile)`** implemented once; Pipeline step, MSBuild target, and CLI delegate to it. Update regenerate hints in emitted headers to canonical command.

---

### P1-8: Hooks only run on Interop + Facade phases today

**Evidence:** No hooks registered for `ImGui`, `Raygui`, `Debug` phases. Spec §5.2 lists `.Hooks(EmitPhase.Interop, EmitPhase.ImGui, EmitPhase.Debug)` — misleading.

**Plug:** Document **phase hook registry per target**; default raylib project: Interop + Facade only. ImGui/Raygui/Debug hooks = extension point for future, not parity requirement.

---

### P1-9: Duplicate inlining path (emitter + hook)

**Evidence:** `FacadeEmitter` emits `[MethodImpl(AggressiveInlining)]` when policy set; `FacadeInliningHook` also adds attribute and strips duplicate from `EndDrawing` rewrite.

**Gap:** Roslyn layer spec implies hooks own adaptation; emitter still embeds policy logic.

**Plug:** Pick one owner for v1 parity:

- **Option A (minimal diff):** keep emitter behavior; hook only patches `EndDrawing` edge case.
- **Option B (clean Roslyn):** emitter emits bare forwards; **`FacadeInliningHook` owns all inlining** — requires parity test update.

Spec should state chosen option; analysis recommends **A for parity**, **B as follow-up**.

---

## P2 — Design tensions & minor inaccuracies

### P2-1: `BindingSurface.Core + ImGui` omits debug + facades grouping

Ergonomic sugar under-specifies which fragments attach to which merge point. `Core` must mean interop + debug; `ImGui` must mean shim + `Gui` façade + companions — not obvious from operator+ alone.

**Plug:** `BindingSurface` presets as **frozen lists of fragment IDs + merge edges**, not runtime bag of fragments.

---

### P2-2: `versions.json` as fragment in emit project

Used only by step_01, not step_06. Including it in `RaylibBindingProject` mix concerns fetch pipeline with codegen graph.

**Plug:** Separate **`SourcePipelineProject`** from **`BindingCodegenProject`**. Link via shared `IPipelineLayout`.

---

### P2-3: Verify merge point only covers raylib.h imports

step_03 does not verify imgui/raygui exports against native binaries — only `raylib-exports` vs header.

**Plug:** Future `VerifyShimExports` step (optional); spec should not imply all fragments are header-verified.

---

### P2-4: `ConflictPolicy.FailOnDuplicateTypeName` across Bindings merge

Raylib6Native, ImguiShimExports, RaylibDebugFrameHooks — no name collision today. Policy matters only if flattening strategies — spec already warns against that. Policy is fine but **untested** for real conflicts.

---

### P2-5: Package split Roslyn dependency weight

`Novolis.CodeGen.Pipeline` consumers may not want Roslyn. Spec lists three packages but Bindings orchestration likely pulls Roslyn transitively if hooks live together.

**Plug:** Dependency graph:

```
Pipeline          (no Roslyn)
Bindings          (no Roslyn — manifests + merge plan only)
Bindings.Roslyn   (Roslyn + hook host)
Bindings.Emit     (optional — ties executor + Roslyn writer)
```

Raylib references Bindings.Emit + domain emitters.

---

### P2-6: Semantic validation open question underspecified

Façade bodies are parsed as strings (`method.Body`). Validating `GuiControls.Setup` requires companion symbol table, not interop table.

**Plug:** Validation layers: L1 syntax parse of body; L2 symbol exists in companion+interop registry; L3 full semantic model (future).

---

### P2-7: Drift step includes paths not in CodegenOutputCatalog

step_07 diffs entire `src/Novolis.Raylib.Bindings/` and `Runtime/` trees — includes hand-written shell code changes, not only generated.

**Plug:** Drift profiles: `generated-only` vs `full-tree`. Agent workflow may need both.

---

## Roslyn-specific stress tests

| Test | Expected | Current risk |
|------|----------|--------------|
| Hook runs after parse, before format | Yes | Yes in `WriteUnit` |
| Façade hook can remove `MethodImpl` from one method | `InjectEndDrawingNotifyHook` strips MethodImpl on EndDrawing | Works; spec’s generic `RewriteMethodBody` helper must support attribute filtering |
| Interop hook adds XML doc without breaking `[LibraryImport]` | AnnotateLibraryImportHook | Must skip methods that already have doc trivia |
| Formatter changes across Roslyn versions | Byte parity fails | Accept T1 AST parity |
| Hook references types not in generated tree | PresentationHooks in Runtime | **Compile-time hook assembly** references Runtime — acceptable for domain hooks, not for library core |

**Plug:** Library hooks (`SyntaxRewriters`) are Roslyn-only and domain-agnostic. Domain hooks stay in consumer assembly and may reference consumer runtime types — document pattern as **“adaptation hooks” vs foundation hooks”**.

---

## Parity checklist (concrete, derived from code)

Use this to gate spec phase 4 (executor wrapping existing emitters):

| # | Output | Manifest input | Phase | Hooks applied |
|---|--------|----------------|-------|---------------|
| 1 | `Bindings/Interop/Raylib6Native.g.cs` | raylib-exports | Interop | AnnotateLibraryImport |
| 2 | `Bindings/Interop/ImguiShimExports.g.cs` | imgui-exports | ImGui | none |
| 3 | `Bindings/Interop/RaylibDebugFrameHooks.g.cs` | raylib-debug | Debug | none |
| 4–10 | `Runtime/{folder}/{Name}.g.cs` ×7 types | facades | Facade | FacadeInlining; Graphics also EndDrawing |
| 11 | `Runtime/Hud/Hud.g.cs` | hud | Facade | FacadeInlining |
| 12 | `Runtime/Gui/Gui.g.cs` | gui | Facade | FacadeInlining |
| 13 | `Raygui/Interop/RayguiShimExports.g.cs` | raygui-exports | Raygui | none |
| 14 | `Raygui/RayGui/RayGui.g.cs` | raygui | Facade | FacadeInlining |

**Not generated (must stay out of parity diff):** companion `.cs` files listed in P1-1, all of `Runtime/Shell`, `Presentation`, etc.

---

## Migration plan stress test

| Spec phase | Hidden dependency | Risk if ignored |
|------------|-----------------|-----------------|
| 1 Extract Pipeline + Roslyn | MSBuild targets still reference old project paths | Local build regenerates with wrong host |
| 2 JSON round-trip | Property order / null omission in enrich vs serialize | Drift false positives |
| 3 BindingProject executor | Must call emitters in **same order** as `GenerateBindingsOnly` if any global state (none today — low) | Low |
| 4 Switch step_06 | CodeGen.targets inputs must include BindingProject sources | Stale generated files |
| 5 C# manifests | Blocked on P0-5 enrich/resolver | Doc workflow breaks for agents |

**Revised order suggestion:**

1. Pipeline + Roslyn extract (unchanged)
2. JSON round-trip tests (per-file serializers)
3. `IBindingCodegenHost` + executor (JSON still authoritative)
4. Parity tests T0 → T1
5. Companion + stack model in spec v2
6. C# authoring for interop/shim/debug only
7. Façade C# authoring after enrich/resolver relocation

---

## Proposed plugs summary (for spec v2)

| ID | Plug |
|----|------|
| P0-1 | Add **Binding stack** (L0–L3) + `CompanionSurface` declarations |
| P0-2 | Per-file wire serializers; shared C# authoring model |
| P0-3 | Scoped template catalogs + `EmbeddedTypes` on shim emitters |
| P0-4 | Context-driven hooks; no disk reads in `Transform` |
| P0-5 | Three-layer doc model; defer façade C# authority |
| P0-6 | Parity tiers T0/T1/T2; default success = T1 |
| P1-6 | `IBindingEmitter` plugin registration |
| P1-7 | Single `IBindingCodegenHost` for Pipeline / MSBuild / CLI |
| P1-2 | NativePack v1 = validate only |
| P2-5 | Split packages to keep Pipeline Roslyn-free |

---

## Open questions (refined)

1. **Companion layer codegen:** Should `GuiControls`-style UTF-8 wrappers ever be partially generated from shim manifest (e.g. string params → `byte*` helpers)?
2. **Raygui namespace:** Keep `Novolis.Raylib.Interop` in Raygui assembly for `InternalsVisibleTo` ergonomics, or migrate to `Novolis.Raylib.Raygui.Interop` (breaking)?
3. **Enrich persistence:** Write derived JSON forever, or move defaults into C# fragment literals and drop enrich writes?
4. **MSBuild codegen default:** Should pack/build always run generate (current) or only CI/maintainer (faster dev loops)?

---

## Next step

Review plugs above; for each accepted plug, add a **delta section to spec v2** (new file `initial-idea-v2.md` or amend after explicit sign-off). Implementation should not start `Novolis.Raylib.Manifests` until **P0-2, P0-6, P1-6, P1-7** have spec language.
