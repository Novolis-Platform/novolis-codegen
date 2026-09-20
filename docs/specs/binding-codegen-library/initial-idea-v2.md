# Binding CodeGen Library — Specification v2

**Status:** implementation baseline (supersedes [initial-idea.md](./initial-idea.md) for build work)  
**Summary:** [summary.md](./summary.md) · **Analysis:** [analysis.md](./analysis.md)

## Locked decisions

| Topic | Decision |
|-------|----------|
| Parity gate | **T1** AST-normalized equivalence |
| Milestone | C-ABI emit library wired through Raylib and Audio |
| Binding model | **L0–L3 stack** with `CompanionDeclaration` |
| Manifest authority | **C# fragments** defined by the consumer (`Novolis.Raylib.Manifests`) |
| IO | **IFileSystem** via `CodegenEnvironment` (tests use `MockFileSystem`) |
| Inlining | Shared façade emitter owns expression-body inlining; hooks retain XML docs + EndDrawing behavior |
| Native scope | C ABI: C libraries and C++ `extern "C"` shims; no C++ class bindings |

## L0–L3 binding stack

| Layer | Role | Raylib examples |
|-------|------|-----------------|
| L0 | Native DLLs | `raylib.dll`, `novolis_imgui.dll` |
| L1 | Generated interop | `Raylib6Native.g.cs`, `ImguiShimExports.g.cs` |
| L2 | Hand companions (declared, not emitted) | `GuiControls`, `ImguiShimHost`, `Utf8StringMarshaller` |
| L3 | Generated façades | `Graphics.g.cs`, `Gui.g.cs` |

Merge points must include **L2 companion declarations** for validation; façades reference companions (`GuiControls.*`), not shim pointers directly.

## Packages (novolis-codegen)

| Package | Contents |
|---------|----------|
| `Novolis.CodeGen.Pipeline` | `IPipelineStep`, `PipelineRunner`, skip/cache, `result.json` |
| `Novolis.CodeGen.Bindings` | Typed `NativeSignature` fragments, C-ABI emitters, `IBindingEmitter`, `BindingCodegenExecutor`, `CodegenEnvironment` |
| `Novolis.CodeGen.Bindings.Roslyn` | `ICodegenHook`, default `BindingCodegenHost`, `RoslynEmitWriter`, `CompilationUnitComparer` |

## Consumer manifests (C#)

Raylib defines fragments in `codegen/Novolis.Raylib.Manifests/*.cs` and exposes them via `RaylibBindingManifestSource.Instance`.

| Fragment kind | Raylib id | Generated output |
|---------------|-----------|------------------|
| `InteropExports` | `raylib6` | `Raylib6Native.g.cs` |
| `ShimExports` | `imgui`, `raygui` | `ImguiShimExports.g.cs`, `RayguiShimExports.g.cs` |
| `DebugConfig` | `raylib-debug` | `RaylibDebugFrameHooks.g.cs` |
| `FacadeTypes` | `facades`, `hud`, `gui`, `raygui` | Runtime / Raygui façades |

Manifest fingerprints use `ManifestFingerprint.Sha256Hex(fragment)` — not JSON serialization.

## API surface

- `IPipelineLayout` — injectable paths (raylib: `RaylibPipelineLayout`)
- `NativeSignature`, `NativeType`, `NativeParameter` — typed C ABI return and named parameter metadata
- `LibraryImportEmitter`, `DynamicExportsEmitter`, `FacadeForwardEmitter` — shared string emitters registered per `EmitStrategy`
- `BindingCodegenHost<TPhase, TContext>` — default consumer job runner over `RoslynEmitWriter`
- `IBindingCodegenHost` — consumer entry (`RaylibBindingCodegenHost`, `AudioBindingCodegenHost`)
- `BindingEmitContext` — **only** config channel for hooks (no disk reads)
- `CompanionDeclaration` — required hand-written files per stack
- `CodegenEnvironment` — `IFileSystem` + repo root for all codegen IO

## Parity

| Tier | Check |
|------|-------|
| T0 | `ManifestSha256` header line (fragment fingerprint) |
| T1 | **Gate:** `CompilationUnitComparer` structural match |
| T2 | Byte-identical (not required) |

14 generated outputs — see [summary.md](./summary.md#parity-scope-14-generated-files).

## Default codegen host

`BindingCodegenHost<TPhase, TContext>` validates companions, filters optional jobs, fingerprints fragments, invokes the selected string emitter, and preserves the existing parse → hook → format → write path. Raylib and Audio hosts only compose jobs, hooks, and consumer-owned verification:

- Pipeline `step_06_codegen`
- MSBuild `GenerateRaylibBindings`
- `Novolis.Raylib.CodeGen generate`

Regenerate hint: `dotnet run --project codegen/Novolis.Raylib.Pipeline -- run generate`

## Implemented consumers

- Raylib uses typed signatures for LibraryImport, ImGui/Raygui dynamic export tables, debug hooks, and one façade job per generated type.
- Audio uses the same LibraryImport and façade emitters for its nine `na_*` imports. Speech and voice catalogs remain Audio-local.

## Deferred

- **Phase 6:** Automated façade doc enrichment back into C# manifest sources
- **Backlog:** NativePack generator and BindingSurface sugar
