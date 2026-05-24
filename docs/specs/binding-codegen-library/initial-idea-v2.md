# Binding CodeGen Library — Specification v2

**Status:** implementation baseline (supersedes [initial-idea.md](./initial-idea.md) for build work)  
**Summary:** [summary.md](./summary.md) · **Analysis:** [analysis.md](./analysis.md)

## Locked decisions

| Topic | Decision |
|-------|----------|
| Parity gate | **T1** AST-normalized equivalence |
| Milestone | Through **Phase 4** (library + raylib wired) |
| Binding model | **L0–L3 stack** with `CompanionDeclaration` |
| Manifest authority | **C# fragments** defined by the consumer (`Novolis.Raylib.Manifests`) |
| IO | **IFileSystem** via `CodegenEnvironment` (tests use `MockFileSystem`) |
| Inlining | Emitter owns inlining; hooks for XML docs + EndDrawing |

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
| `Novolis.CodeGen.Bindings` | Fragments, `IBindingManifestSource`, `IBindingEmitter`, `BindingCodegenExecutor`, `CodegenEnvironment` |
| `Novolis.CodeGen.Bindings.Roslyn` | `ICodegenHook`, `RoslynEmitWriter`, `CompilationUnitComparer` |

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
- `IBindingEmitter` — domain string emitters registered per `EmitStrategy`
- `IBindingCodegenHost` — consumer entry (`RaylibBindingCodegenHost`)
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

## Single codegen host

All entry points delegate to `RaylibBindingCodegenHost`:

- Pipeline `step_06_codegen`
- MSBuild `GenerateRaylibBindings`
- `Novolis.Raylib.CodeGen generate`

Regenerate hint: `dotnet run --project codegen/Novolis.Raylib.Pipeline -- run generate`

## Deferred

- **Phase 6:** Automated façade doc enrichment back into C# manifest sources
- **Backlog:** NativePack generator, BindingSurface sugar, second consumer
