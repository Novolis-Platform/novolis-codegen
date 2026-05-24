# Binding CodeGen — Analysis Summary

**Start here.** Full detail: [analysis.md](./analysis.md) · Original spec: [initial-idea.md](./initial-idea.md)

---

## One sentence

The spec’s **fragments + merge points + Roslyn hooks** shape is right, but it models a **flat** pipeline; raylib is a **stacked** pipeline with hand-written middle tiers, split JSON shapes, and three codegen entry points — fix the model before writing the library.

---

## Spec vs reality (at a glance)

```mermaid
flowchart TB
  subgraph spec ["What the spec describes"]
    direction LR
    M[Manifest fragments]
    MP[Merge points]
    E["Emit g.cs files"]
    H[Roslyn hooks]
    M --> MP --> E --> H
  end

  subgraph gaps ["Spec gaps"]
    direction TB
    G1[Missing companion layer]
    G2[Unified JSON assumed]
    G3[Single codegen host assumed]
  end

  subgraph reality ["What raylib actually has"]
    direction TB
    JSON[8 JSON files + versions.json]
    EMIT[5 string emitters]
    COMP[Hand companions]
    HOOK[3 hooks on 2 phases]
    MSB[MSBuild + Pipeline + CLI]
    JSON --> EMIT
    EMIT --> COMP
    EMIT --> HOOK
    MSB --> EMIT
  end

  spec --> gaps
  gaps --> reality
```

| Area | Spec assumption | Reality |
|------|-----------------|---------|
| GUI binding | Façade → shim exports | Façade → **Controls** → host → shim |
| Manifests | One C# model → JSON | **3 wire formats** (`imports` / `functions` / debug) |
| Templates | Global `Template.*` | **3 disjoint** template tables + emitter extras |
| Docs | C# authoritative | JSON + **enrich writes** + hardcoded resolver defaults |
| Parity | Byte-identical emit | Tests check **SHA256 line + snippets** only |
| Codegen | Pipeline step_06 | **Pipeline + MSBuild + CLI** |

---

## The binding stack (main thing the spec missed)

Raylib core is simple (façade calls `Raylib6Native` directly). GUI paths are not.

```mermaid
flowchart TB
  subgraph L0 ["L0 Native DLLs"]
    RDLL[raylib.dll]
    IDLL[novolis_imgui.dll]
    GDLL[novolis_raygui.dll]
  end

  subgraph L1 ["L1 Generated interop"]
    RN[Raylib6Native.g.cs]
    IS[ImguiShimExports.g.cs]
    RS[RayguiShimExports.g.cs]
  end

  subgraph L2 ["L2 Hand companions - not in spec"]
    MAR[Marshalling and RaylibColor]
    IH[ImguiShimHost]
    GC[GuiControls]
    RGC[RayGuiControls]
  end

  subgraph L3 ["L3 Generated facades"]
    FAC[Graphics, Window, etc]
    GUI[Gui.g.cs]
    RG[RayGui.g.cs]
  end

  RDLL --> RN
  IDLL --> IS
  GDLL --> RS

  RN --> MAR
  IS --> IH --> GC
  RS --> RGC

  MAR --> FAC
  RN --> FAC
  GC --> GUI
  RGC --> RG
```

**Takeaway:** merge points must declare **L2 companions**, not only L1 + L3.

---

## Issue heatmap (5 themes, not 20 tickets)

```mermaid
flowchart TB
  subgraph fixfirst ["Fix first - high urgency"]
    direction LR
    N1[Binding stack L0-L3]
    N2[Emitter plugin API]
    N3[Single codegen host]
    N4[JSON serializers]
    N5[Parity tiers T0-T1]
  end

  subgraph v2 ["Plan in spec v2"]
    V1[Doc enrich model]
  end

  subgraph backlog ["Defer"]
    direction LR
    B1[NativePack fluent API]
    B2[BindingSurface sugar]
    B3[NuGet package split]
  end

  fixfirst --> v2 --> backlog
```

| Theme | Count | Severity | One-line fix |
|-------|-------|----------|--------------|
| **A. Wrong shape** — stack, JSON, templates | 3 P0 | Critical | Add L0–L3 stack + per-file serializers + scoped templates |
| **B. Library skeleton** — emitters, hosts, parity | 4 P1 | High | `IBindingEmitter`, unified host, parity T0→T1 |
| **C. Hooks & Roslyn** — context, phases, duplicate inlining | 3 P0/P1 | High | Context-only hooks; document 2 active phases |
| **D. Docs & authority** — enrich, resolver, C# manifests | 1 P0 | Medium-high | Defer façade C#; three-layer doc model |
| **E. Polish & future** — NativePack, BindingSurface, packages | 6 P2/P1 | Low | Validate-only NativePack v1; split NuGet packages |

---

## All issues — compact card view

### Must fix before library design freezes (P0)

| ID | Issue | Plug |
|----|-------|------|
| P0-1 | GUI is 4 layers, not façade→shim | `CompanionSurface` + L0–L3 stack in spec |
| P0-2 | Shim JSON ≠ interop JSON on disk | Shared C# model, **split serializers** |
| P0-3 | 3 template vocabularies + embedded structs | `InteropTemplate` / `ImGuiTemplate` / `RayguiTemplate` |
| P0-4 | Hooks read JSON from disk | Pass config via **`BindingEmitContext` only** |
| P0-5 | Enrich writes JSON; resolver has defaults | Keep JSON authoritative for façades in v1 |
| P0-6 | Byte parity too strict | Success = **T1 AST parity** (not byte-identical) |

### Fix during implementation (P1)

| ID | Issue | Plug |
|----|-------|------|
| P1-1 | Hand interop files not tracked | `RequiredCompanions` in project |
| P1-2 | NativePack is hand csproj | v1 validate paths only |
| P1-3 | Raygui optional is tri-state | Single `IncludeRaygui` precedence chain |
| P1-4 | Raygui shim namespace ≠ assembly | Explicit `EmitTarget.Namespace` |
| P1-5 | “Folders mirror names” wrong | Folder ≠ type name (World → Rendering/) |
| P1-6 | No emitter plugin API | `IBindingEmitter` + registration |
| P1-7 | 3 codegen entry points | `IBindingCodegenHost` |
| P1-8 | Hooks on ImGui/Debug unused | Document; only Interop + Facade today |
| P1-9 | Inlining in emitter **and** hook | Keep emitter for parity (option A) |

### Can wait (P2)

NativePack generator · `BindingSurface` presets · separate Source vs Codegen project · shim export verify · drift profiles · NuGet dependency split · semantic validation layers.

---

## What still holds from the original spec

```mermaid
flowchart TB
  K[Keep from original spec]
  K --> F1[Fragments with stable Id]
  K --> F2[Merge points explicit]
  K --> F3[Roslyn post-emit hooks]
  K --> F4[JSON as drift artifact]
  K --> F5[Separate partial classes per DLL]
  K --> F6[Pipeline step fingerprints]
  K --> F7[Optional Raygui branch]
```

Do **not** throw away: fragment IDs, merge points, hook pipeline, “don’t flatten LibraryImport + dynamic exports into one class.”

---

## Recommended path (visual)

```mermaid
flowchart TD
  START["Today: JSON + emitters work"] --> PH1["Phase 1: Extract Pipeline + Roslyn"]
  PH1 --> PH2["Phase 2: JSON round-trip tests"]
  PH2 --> PH3["Phase 3: IBindingEmitter + unified host"]
  PH3 --> PH4["Phase 4: Parity T0 then T1"]
  PH4 --> DEC{"Adopt binding stack in spec?"}
  DEC -->|yes| PH5["Phase 5: C# manifests - interop/shim/debug"]
  DEC -->|no| ADHOC["Raylib-only wrappers - tech debt"]
  PH5 --> PH6["Phase 6: Facade C# after doc model"]

  style DEC fill:#ffe599
```

| Phase | Outcome | Blocks on |
|-------|---------|-----------|
| 1 | Shared pipeline kernel | — |
| 2 | Proves manifest model | — |
| 3 | Library can orchestrate raylib emitters | P2 |
| 4 | Safe to swap step_06 | P3 |
| 5 | C# authoring (partial) | **P0-1 stack + P0-5 docs** |
| 6 | Full C# façades | Enrich/resolver decision |

---

## Parity scope (14 generated files)

```mermaid
flowchart LR
  subgraph bindings ["Bindings assembly"]
    B1[Raylib6Native]
    B2[ImguiShimExports]
    B3[RaylibDebugFrameHooks]
  end

  subgraph runtime ["Runtime assembly"]
    R1["7 facade types"]
    R2[Hud]
    R3[Gui]
  end

  subgraph raygui ["Raygui add-on - optional"]
    Y1[RayguiShimExports]
    Y2[RayGui]
  end

  bindings --> runtime
  raygui -.->|"IncludeRaygui"| runtime
```

**Out of parity diff:** all hand-written `.cs` (companions, shell, presentation hooks).

---

## Decisions to make next (pick 1–2)

| # | Question | Options |
|---|----------|---------|
| 1 | Binding stack in spec v2? | **Yes (recommended)** / defer |
| 2 | Parity bar | **T1 AST** / T0 headers only / T2 byte |
| 3 | Façade authority in v1 | **JSON** / C# |
| 4 | First library deliverable | **Pipeline extract** / Bindings model / Roslyn host |

---

## Document map

| File | Role |
|------|------|
| [initial-idea.md](./initial-idea.md) | Original design (read-only baseline) |
| [initial-idea-v2.md](./initial-idea-v2.md) | **Implementation spec** (L0–L3, APIs, parity) |
| **summary.md** (this file) | Visual overview + priorities |
| [analysis.md](./analysis.md) | Full evidence, code references, plug details |

When a plug is accepted, record it in **initial-idea-v2.md** — not by editing the baseline spec until sign-off.
