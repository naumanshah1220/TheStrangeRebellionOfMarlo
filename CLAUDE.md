# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Strange Rebellion of Marlo** — A detective game built in Unity 6 (6000.0.32f1) using URP. The player investigates cases by interrogating suspects, analyzing evidence with tools, collecting clues, and submitting verdicts.

## Unity MCP

This project uses MCP for Unity. Always check `mcpforunity://custom-tools` for project-specific tools. After creating or modifying scripts, use `read_console` to verify compilation before proceeding. Poll `editor_state` resource's `isCompiling` field to check domain reload status.

## Architecture

### Game State Flow
`GameManager.GameState`: Boot → DailyStart → CaseSelection → CaseActive → CaseClosed → DayComplete → GameEnd

### Manager Singletons (Scripts/Core/Managers/)
All managers use the `Instance` singleton pattern. Key managers and their responsibilities:

- **GameManager** — Master state machine, orchestrates all other managers, exposes events (`OnCaseOpened`, `OnCaseClosed`, `OnDayStarted`, `OnDayEnded`)
- **CaseManager** — Case pool, progression, status tracking
- **CaseProgressionManager** — Unlocks additional simultaneous case slots as cases are solved
- **InterrogationManager** — Interrogation logic; **ChatManager** handles the UI (WhatsApp-style chat). Architecture comment in source: "InterrogationManager handles logic, ChatManager handles UI"
- **NotebookManager** — Book-style notebook: Page 1 = suspects, Page 2+ = clues
- **CardTypeManager** — Routes cards to appropriate hands based on `CardMode`
- **DragManager** — Card drag-and-drop across hands/mat/slots
- **EvidenceManager** / **CluesManager** / **SuspectManager** — Data management for their respective domains
- **ToolsManager** — Registry for investigation tools
- **DaysManager** — Daily progression and case availability
- **UIManager** — UI state and animation coordination
- **BookManager** — Book/evidence reader system
- **OverseerManager** — Overseer narrative notes

### Card System (Scripts/Gameplay/Cards/)
- `Card` — Base draggable class implementing `IDragHandler`, `IBeginDragHandler`, `IEndDragHandler`, etc. Uses DOTween for animations.
- `CardMode` enum: Case, Evidence, Phone
- `CardVisual` — Hand view; `BigCardVisual` — Mat (expanded) view
- `HorizontalCardHolder` — Card layout container with free-form placement support
- `ICardData` interface standardizes data across card types
- `EnhancedCardVisual` — Torn/connectable card piece mechanics

### Interrogation Pipeline
```
Player drags tag → InterrogationDropZone
  → InterrogationManager.ProcessTagResponseDirectly()
    → Citizen.GetResponseForTag(tagId) → TagInteraction lookup
      → TagResponse (isLie flag, delay, clickable clue segments)
        → ChatManager.ShowSuspectResponse() (typewriter effect, auto-fade)
```
Truth-unlock mechanic: presenting contradicting evidence unlocks truthful responses.

### Tool System (Scripts/Gameplay/Tools/)
Base `Tool` class with specialized implementations:
- **Computer** (`ComputerSystem`) — Desktop/window manager with apps (CitizenDatabase, ImageViewer, DocumentViewer, VideoPlayer, Folder). Uses `AppWindow`/`AppConfig` framework. Disc evidence detection.
- **FingerPrintDuster** (`FingerPrintDusterSystem`) — Brush powder application, mask reveal, LCD display, light switch for retro glow
- **Spectrograph** (`SpectrographSystem`) — ROYGBIV spectrum analysis, `ForeignSubstanceDatabase` for substance lookup

### Verdict System (Scripts/Gameplay/Cases/)
- `Case` ScriptableObject contains `VerdictSchema` (answer slots) and `CaseSolution[]` (multiple valid solutions)
- `VerdictEvaluator` — Static utility computing confidence score
- `JustificationDropZone` — Players attach evidence/clues to support their verdict

### Data Layer (ScriptableObjects)
All game content is data-driven via ScriptableObjects:
- `Case` — Case data, evidence links, solutions, prerequisites
- `Citizen` — Name, DOB, criminal record, 5 fingerprints, `TagInteraction[]` for knowledge, `GenericQuestion[]`/`GenericResponse[]` fallbacks, nervousness level
- `Evidence` — Physical evidence items per case
- `AppConfig` / `DiscFile` — Computer app and file representations
- `OverseerNotes` — Narrative progression
- Test case assets live in `Scripts/Core/Data/Cases/` (Core 1-4, Secondary 1-3)

## Key Directories

```
Assets/Scripts/Core/Managers/     # 21 singleton managers
Assets/Scripts/Core/Data/         # Data classes, case assets, clue/dialogue types
Assets/Scripts/Gameplay/Cards/    # Card system (drag, visual, holder)
Assets/Scripts/Gameplay/Tools/    # Computer, FingerPrintDuster, Spectrograph
Assets/Scripts/Gameplay/Cases/    # Verdict, solution evaluation
Assets/Scripts/UI/Chat/           # Interrogation chat UI
Assets/Scripts/UI/Notebook/       # Notebook pages UI
Assets/Scripts/UI/Commit/         # Verdict submission UI
Assets/Scripts/Debug/             # Test helpers (card, computer, book)
Assets/Prefabs/                   # Organized by system (Cards, Clues, Tools, etc.)
Assets/Resources/                 # Runtime-loadable assets (Fingerprints, Portraits)
Assets/Scenes/                    # Detective 2.0-6.0, Balatro-Feel, Ortho
```

## Dependencies

- **DOTween** (Demigiant) — Animation tweening, used heavily in Card and UI systems
- **TextMeshPro** — All text rendering
- **SimpleScrollSnap** — Scroll snap UI components
- **New Input System** (1.11.2)
- **URP** (17.0.3)
- **Newtonsoft JSON** — Serialization
- **Flexalon** — Layout system

## Scenes

The active development scene is **Detective 6.0**. Earlier versions (2.0-5.0) are archived iterations. **Balatro-Feel** is a UI style experiment. **Ortho** is an orthographic view test.

## Conventions

- Managers find each other via `FindAnyObjectByType<T>()` in `GameManager.FindAllManagers()`
- Clue text supports inline tags: `<person>Name</person>`, `<location>Place</location>` — processed by `ClueTextProcessor`
- CSV integration via `CSVReader` for citizen data population
- All asset paths use forward slashes, relative to `Assets/`
- No namespaces used — all scripts are in the global namespace


## 0) THE GAME (explain it clearly)

### High concept
You are **Marlo**, a Pattern Analyst for an authoritarian regime called **The Council**.
Citizens are policed via a doctrine called **The Pattern** (Seven Sins reinterpreted as state crimes).
You process cases at a desk: examine evidence, discover tags, interrogate suspects, and submit an indictment form.
You are not judge/jury — you are the regime’s paperwork blade.

### Core tensions
- **Survival**: salary vs bills, family needs, random fees
- **Compliance**: Council Favor, promotions, tool access
- **Conscience**: Resistance Trust, moral choices, branching outcomes

### The “feel”
Like **Papers, Please**:
- tactile drag/drop and slotting
- satisfying rituals (indict/dispatch)
- time pressure and throughput
- **delayed truth** (results revealed next day via newspaper), but **immediate tactile closure** via indictment ritual

### Playtime targets
- Story Mode: **30 days**, ~10–20 hours total
- Post-story Free Play: endless “whodunnit” cases assembled procedurally from curated components

---

## 1) GOALS / NON-GOALS (v1)

### Goals
- Build a complete playable game in Unity with a robust **data-driven JSON pipeline**
- Implement 30-day Story Mode day loop + post-story Free Play hook
- Preserve tactile desk mechanics and tool feel
- Implement delayed feedback (newspaper next day), but immediate indictment ritual

### Non-goals (v1)
- No voice acting (text only)
- No complex 3D environments (UI-driven 2D presentation)
- No online leaderboard

---

## 2) REQUIRED GAME STATES (Flow Spine)
The project must implement an explicit state machine.

Minimum states:
- Boot
- MainMenu
- LoreSlideshow (Intro)
- DayBriefing (newspaper + family letter + unlock notices)
- Workday (desk gameplay loop)
- NightSummary (pay, bills, events, save)
- Endings (story completion)
- FreePlay (post-story)

Rules:
- Mechanics/tools do NOT control flow.
- Mechanics/tools emit events.
- FlowController owns transitions, loading/unloading screens, and RunContext.

---

## 3) CORE LOOP (Day Loop)

### Day start
- Player presses Start Day
- Time starts ~06:00
- DaysManager loads DayDefinition (JSON)
- Case queues created:
  - coreQueue (must complete)
  - secondaryQueue (optional but needed for money)
- Active cases appear in Case Hand up to activeCaseCap and maxCoreActive

### Workday loop
Repeat:
- Drag case card from Case Hand → Mat to open
- Evidence Hand slides in (case evidence cards)
- Page through evidence, click hotspots to log clues
- Notebook unlocks tags; drag tags to tools (interrogation, DB search, fingerprint, spectrograph)
- Complete suspect profile bits; call suspect to interrogation when allowed
- Press Indict → fill form → submit (dispatch ritual) → case closes
- Next case enters Case Hand from queue as space frees

### Day end rules
- End Day disabled if coreQueueRemaining > 0
- If time hits 18:00 with core remaining → Overtime:
  - stress increases
  - penalties increase
  - family letters worsen

### Night summary
- salary + bonuses
- penalties (wrong indictments, overtime, incomplete core)
- bills + random events
- player chooses payments
- update narrative vars (CouncilFavor / ResistanceTrust)
- save

---

## 4) UI/UX (Portrait Acceptance Regions)
- Top Bar: time, pause, End Day (disabled if core remaining), alerts
- Tools Bar: horizontal carousel, snap panels, drop zones
- Desk / Mat: primary inspection area (paging + hotspots)
- Overseer Hand: system items (letters, permits, discs)
- Case/Evidence Hand: case cards + evidence cards
- Right Buttons: Notebook overlay, Indict overlay

Cards are slot-based: invisible slots move; visuals follow.
Small visual in hands; big visual on mat/tool slots.

---

## 5) NOTEBOOK + TAGS (Gating)
Tags are the language of the game.
- Tools only accept relevant tag types.
- No arbitrary typing into DB (v1): only tag drops.

Notebook is case-scoped (switching case updates content).

---

## 6) TOOLS (Key requirement)
Existing tools likely already exist and should remain stable.
Formalize behavior, integrate via adapters/events, and enhance missing logic.

Interrogation must become graph-driven:
- nodes, conditions, outcomes
- contradictions: claims can be contradicted by evidence tags
- stress escalation + variants to avoid monotony

---

## 7) JSON-FIRST CONTENT PIPELINE (StreamingAssets)
All content loads from StreamingAssets JSON into runtime models.

Required JSON domains (minimum):
- days.json
- cases.json
- evidence.json
- clues.json
- dialogue.json
- citizens.json (or equivalent)

Required tools:
- Case validator report viewer
- Force load case dropdown
- Reveal hotspots toggle (authoring/debug)

Build order:
1) Load hand-authored JSON cases
2) Validator (editor + runtime)
3) Generator (template/pool-based) only after validator is stable

---

## 8) CRITICAL REALITY OF THIS REPO
- Many mechanics/tools already work as desired.
- What is missing is FLOW + MENU + SLIDESHOW + GENERATOR + VALIDATOR + BETTER INTERROGATION.

Therefore:
✅ Preserve working mechanics.
✅ Build Flow Spine + Content Spine above them.
✅ Only touch mechanics when justified by integration needs.

---

## 9) MANDATORY WORKFLOW (Claude must follow)

### Phase 0 — READ-ONLY STUDY (NO CODE CHANGES)
Produce a Repo Understanding Report:
1) Inventory (scenes, prefabs, managers, stable mechanics/tools)
2) Current flow (what drives the loop now)
3) Dependency map (coupling points)
4) Freeze list (keep as-is / adapter-wrap / risky defer)
5) Slice plan (below) with file list + acceptance + Unity test steps

STOP after Phase 0.

### Phase 1+ — Refactor by vertical playable slices (NO big rewrites)
Slice 1: MainMenu → Start → Workday (DeskLoop wrapper)
Slice 2: LoreSlideshow + DayBriefing → Workday
Slice 3: CaseData JSON → Loader into existing tools/mechanics
Slice 4: Indict → CaseComplete → NightSummary + Newspaper
Slice 5: Validator (editor + runtime)
Slice 6: Generator (pool/template)
Slice 7: Interrogation Graph + contradiction system

Each slice must end in a runnable Unity state.

---

## 10) REQUIRED TASK OUTPUT FORMAT (per slice)
1) Goal
2) Files added/changed
3) Implementation steps (ordered)
4) Acceptance checklist (binary)
5) Unity test steps (exact)
6) Rollback plan

---

## DO NOT
- No repo-wide renames or “cleanup refactors”
- No rewriting tools that already work
- No huge scene/prefab rewrites in one pass
- No third-party packages unless necessary