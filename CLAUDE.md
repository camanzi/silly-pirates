# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workflow

Before implementing any change that requires more than a single edit (anything beyond a label, rename, or trivial fix), enter plan mode and show the plan for approval before touching any file.

During planning, if there are any implementation doubts — design decisions, ambiguous requirements, trade-offs between approaches — ask the user before finalising the plan. Never make large assumptions; always have all necessary information before exiting plan mode.

## Project Overview

**Silly Pirates** is a tactical turn-based combat game built in Unity (URP). Characters occupy a hexagonal grid, take turns spending action points and movement points on abilities, and interact through a ScriptableObject-driven event architecture.

## Unity & Build

This is a Unity project — there is no CLI build command for day-to-day development. Open the project in the Unity Editor and use the standard Play/Build workflow. The Unity MCP integration (`unity-mcp` package) allows Claude Code to interact with the running Editor directly via MCP tools.

**Assemblies:** all runtime code compiles into `SillyPirates.Runtime`
(`Assets/Scripts/Runtime/SillyPirates.Runtime.asmdef`); the two Editor scripts in `Assets/Editor/` stay in
`Assembly-CSharp-Editor`. Consequences: a script created **outside** `Assets/Scripts/Runtime/` is no longer
visible to the rest of the game, and a new package dependency must be added to the asmdef's `references`
or the project will not compile.

**Python utility (sprite sheet extraction):**
```
python UtilityScripts/animation_extractor.py
```

## Core Architecture

### Turn System

`TurnController.cs` drives the main game loop using Unity's `Awaitable` async pattern. It dequeues turn agents, awaits their `ExecuteTurn()`, then advances. Each agent's stats (AP, movement points, agility) are stored in a `TurnAgentDataSO` ScriptableObject. Characters implement `ITurnAgent` to participate in the queue.

### Combat State Machine

`CombatStateManager.cs` runs a three-state FSM: **Idle → Targeting → Execution**. States are ScriptableObject instances (`CombatStateSO`) cloned at runtime for isolation. `CombatContext` carries the active ability and targeting data across state transitions.

### Ability System

Abilities are ScriptableObjects extending `AbilityBase`, which defines three key methods:
- `GetPreviewData()` — computes the set of affected grid cells
- `CreateCommand()` — returns an `ICommand` for execution
- `CanExecute()` — validates whether the ability can fire

AoE shapes are defined via `IAreaShape` implementations (`CircleShape`, `LineShape`), keeping shape logic separate from ability logic. Equipment-bound abilities go through `ShootWithEquipmentAbility`.

**Enemy abilities** extend `EnemyAbilityBase : AbilityBase`, which adds a fourth method:
- `Score(AIContext context, out TargetingData targeting)` — evaluates how desirable this ability is given the current game state, and outputs the chosen target. Returns `float.NegativeInfinity` if the ability is not applicable (turn passes).

`AIContext` carries the caster (`HostileCharacter`), `TurnOrderDataSO` (for iterating active agents by team), and `GridStateDataSO`. Enemy ability assets live under `Assets/Data/Abilities/Enemy/`.

### Enemy AI

Enemy turns are driven by `EnemyTurnDriver` (MonoBehaviour on the enemy prefab), which bridges the `com.unity.behavior` Behavior Tree with the turn system.

Turn flow:
1. `HostileCharacter.OnStartingTurn()` calls `EnemyTurnDriver.ExecuteTurnAsync()`
2. The driver injects runtime references into the BT Blackboard and calls `Restart()`
3. The BT runs two sequential custom nodes:
   - `EvaluateAndSelectAbilityAction` — calls `Score()` on every `EnemyAbilityBase` in the `EnemyAIDataSO`, picks the highest score, writes result to the Blackboard
   - `ExecuteSelectedAbilityAction` — reads the Blackboard, calls `CanExecute` / `CreateCommand` / `AddCommand` on the chosen ability
4. After the BT completes (Success or Failure), the driver calls `ProcessQueueAsync()` then `SignalTurnEnd()` — guaranteed via `finally`, so the turn always ends

`EnemyAIDataSO` is the per-enemy-type configuration SO (ability list + TurnOrderDataSO + GridStateDataSO refs). It lives on `EnemyTurnDriver` as a `[SerializeField]`. Enemies execute exactly **one ability per turn** and do **not move**. If no ability scores above `NegativeInfinity`, the turn passes silently.

Scripts: `Assets/Scripts/Runtime/Combat/AI/` — BT nodes: `Assets/Scripts/Runtime/Combat/AI/BT/`

### Command Pattern

Commands (`ICommand`) are queued in `TurnController` and executed asynchronously. Commands support undo. This is the sole path for mutating game state during a turn.

### Grid

`GridStateDataSO` tracks per-cell occupancy for multiple entity types. `ShipController` manages the tilemaps and renders movement/ability highlights by swapping tiles. `PathFindingUtils` implements A* for hexagonal grids (odd/even row offset coordinates) and exposes reachable-area calculation.

Player characters extend `GridElement` (position + occupancy registration) → `InteractableGridElement` (click, selection, proximity) → `GridCharacter`. `HostileCharacter` is a separate `MonoBehaviour` that implements the same interfaces directly (`IInteractableElement`, `ITargettable`, `ITurnAgent`, `IHealthOwner`) but does **not** extend `GridElement`.

### Event Channel Pattern

Loose coupling is achieved through typed ScriptableObject event channels (`GenericEventChannelSO<T>`). Systems raise events on channels; listeners (`GenericEventChannelListenerSO<T>`) subscribe without direct references. Key channels: `TurnAgentEventChannel`, `HighlightGridEventChannel`, `InteractableElementEventChannel`.

### Input

`InputReader.cs` wraps Unity's New Input System (auto-generated `GameInput`). `WorldInteractor.cs` raycasts to detect hover/click on `IClickable` objects. `GridInputHandler.cs` converts raw click positions into `TargetingData` for the state machine.

### UI

UI uses **UI Toolkit** (UIElements). `InteractionMenuController` renders the radial ability menu with **PrimeTween** animations. HUD components (`ActionPointController`, `CrewOverviewController`, `TurnOrderController`) update via event channels.

### Sprite Animation

`DirectionalSpriteController.cs` drives an 8-directional (N/NE/E/SE/S/SW/W/NW) frame animation system. Sprites are cached from atlases via `SpriteAtlasHelper`. Direction is computed relative to the camera each frame.

## Testing

Unity Test Framework 1.6.0, run from the open Editor via MCP. There is no CI.

| Assembly | Path | Notes |
|---|---|---|
| `SillyPirates.Runtime` | `Assets/Scripts/Runtime/` | All game code; what the tests reference |
| `SillyPirates.Tests.EditMode` | `Assets/Tests/EditMode/` | Editor-only, `nunit.framework.dll` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner` |
| `SillyPirates.Tests.PlayMode` | `Assets/Tests/PlayMode/` | Same minus `UnityEditor.TestRunner` |

Both test assemblies carry `"defineConstraints": ["UNITY_INCLUDE_TESTS"]` — that is what keeps them out of
game builds. Never drop it.

**Running:** `mcp__UnityMCP__run_tests(mode: "EditMode")` returns a `job_id`; poll it with
`mcp__UnityMCP__get_test_job(job_id, wait_timeout: 60, include_failed_tests: true)`. PlayMode needs
`init_timeout: 120000` (domain reload). A job orphaned by a domain reload blocks later runs — clear it with
`run_tests(clear_stuck: true)`. The `mcpforunity://tests` resource lists what the runner discovered.
Filtering by `test_names` does **not** match a parameterized test by its method name — use `assembly_names`.

**PlayMode tests and Enter Play Mode Options:** with *Reload Domain* disabled
(`m_EnterPlayModeOptions: 1` in `ProjectSettings/EditorSettings.asset`) a PlayMode run never initializes —
it enters play mode, hangs in transition, and the job auto-fails on timeout, leaving an orphan
`Assets/InitTestScene*.unity` behind. The project's committed setting is `m_EnterPlayModeOptions: 0`, which
works; the MCP test runner manipulates this setting around runs and an interrupted run can leave it wrong,
so check it if PlayMode suddenly stops initializing.

**Why the runtime asmdef exists:** assembly definition files cannot reference the predefined assemblies, so
while all game code lived in `Assembly-CSharp` no EditMode test could see it. Note the cost, in case another
assembly is ever needed: moving a type between assemblies invalidates every assembly-qualified reference
stored in assets — `m_TargetAssemblyTypeName` (UnityEvent persistent calls), Behavior Tree `RuntimeTypeString`
/ `m_SerializableType`, and `UxmlSerializedData`. UXML regenerates on a forced reimport; the rest has to be
rewritten by hand. `m_EditorClassIdentifier` is only a hint and needs no fixing.

**Testable vs not:** pure static logic is fair game (`MathUtils`, `PathFindingUtils` — already
dependency-injected via `Func` delegates, so a whole A* runs against a lambda grid — and the `bool[,]`
helpers in `GridUtils`). Not unit-testable: anything needing a `Tilemap` (`GridUtils.FindInnerArea`), UI
Toolkit visuals, tween timings, VFX, camera framing, and the enemy `ComputeScore` implementations (they walk
live transforms and `SlimyBallAbility` uses RNG). See `.claude/agents/qa-engineer.md` for the full map.

**Coverage:** `com.unity.testtools.codecoverage` 1.3.0, **Window > Analysis > Code Coverage**.

Verified behaviour, so nobody has to rediscover it: **a test run launched via MCP produces no coverage
report unless "Enable Code Coverage" is already ticked in that window.** There is no public API for the
toggle (`UnityEditor.TestTools.CodeCoverage.CodeCoverage` only exposes `StartRecording`/`StopRecording`, and
setting `UnityEngine.TestTools.Coverage.enabled` turns on engine instrumentation without attaching the
reporter — confirmed: a full EditMode run with it on wrote nothing). So coverage needs one human click
first; afterwards the setting persists in preferences. The headless alternative is the CLI, which requires
the Editor closed since one instance locks the project:

```
Unity.exe -projectPath <path> -batchmode -testPlatform editmode -runTests -debugCodeOptimization   -enableCodeCoverage -coverageResultsPath <path>   -coverageOptions "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:+SillyPirates.Runtime;pathFilters:-**/Tests/**"
```

Accurate data requires Code Optimization on *Debug* (this project is already in Debug — check
`CompilationPipeline.codeOptimization` rather than assuming). Include `SillyPirates.Runtime` only, never the
test assemblies, or the tests measure themselves. Output lands in `CodeCoverage/` at the project root
(gitignored); read the OpenCover XML. Coverage is for finding unwalked branches, not a percentage to chase.

## Design Reference

Mockup su Figma: board **"Silly Pirates - Mockups"**, fileKey `gOTH97KrEjOmsApwDI4FI7`, pagina unica `0:1`.
Passare il fileKey ai tool del Figma MCP (`get_metadata`, `get_design_context`, `get_screenshot`).

| Canvas | node-id |
|---|---|
| Main Menu | `136:117` (dentro: `Moving Group` `137:127`, gli 8 pezzi del drago) |
| Base Button (component set Default/Selected) | `137:134` |
| Combat - Overview - Idle | `5:104` |
| Combat - Overview - Captain abilities selection | `16:91` |
| PG - Detail | `1:2` |
| Components (libreria) | `3:19` |

Due trappole viste sul campo:
- `get_metadata` su alcuni frame (fra cui `136:117`) torna il frame **senza figli**. Non vuol dire che
  sia appiattito: interrogare direttamente il nodo figlio, o usare `get_design_context`.
- Gli export PNG di Figma sono @2x e quasi sempre **non sono potenze di due**. Unity di default li
  riscala alla POT piu' vicina (`nPOTScale: 1`), il che cambia l'aspect ratio e sposta il contenuto
  dentro l'elemento UI Toolkit. Per gli sfondi importati da Figma va messo `nPOTScale: 0`.

## Key Packages

| Package | Purpose |
|---|---|
| PrimeTween (local plugin) | Tween animations (movement, UI) |
| Cinemachine v3 | Camera management |
| Input System v1.19 | New input system |
| URP v17 | Rendering pipeline |
| Behavior Tree v1.0 | Enemy AI |
| unity-mcp (git) | MCP ↔ Unity Editor bridge |
| Test Framework v1.6 | Automated tests (EditMode/PlayMode) |
| Code Coverage v1.3 | Coverage measurement for `SillyPirates.Runtime` |

## Specialized Agents

Project-specific agents are in `.claude/agents/` — invoke with `@<name>` in the chat:

| Agent | When to use |
|---|---|
| `@performance-checker` | Review code that runs in Update/LateUpdate or is called frequently — catches GC allocations, closure captures, expensive per-frame calls |
| `@ui-builder` | Build new HUD or world-space UI elements, including from Figma — knows the UXML/USS/UxmlElement patterns and PrimeTween conventions |
| `@system-builder` | Design and implement a new game system — enforces SO-first data, event channels, `Awaitable` async, and interface contracts |
| `@ability-designer` | Create new combat abilities — knows the `AbilityBase` three-method contract, shape system, command pattern, and asset wiring |
| `@sound-implementer` | Wire SFX, ambience and music into gameplay — knows `AudioDirector`, `SoundEventSO`, the three cue channels and the pooling rules. Implements sounds, never creates them |
| `@qa-engineer` | Write and run automated tests, pin a bug with a failing test, or report coverage gaps — owns the test assemblies, the MCP run loop and the testability map |
| `@camera-director` | Decide/tune how abilities are framed on screen — owns the camera cue system (`CameraCueType`, `CameraCueProfileSO`, `CameraDirector`), assigns cues to ability assets, extends it (FrameArea, shake, FollowProjectile) |

## Coding Conventions

- Async operations use Unity's `Awaitable` (not `Task`) with `CancellationToken` propagation.
- ScriptableObjects are the primary data/configuration container; avoid duplicating state in MonoBehaviours.
- New player abilities: extend `AbilityBase` and place the asset under `Assets/Data/Abilities/`.
- New enemy abilities: extend `EnemyAbilityBase`, implement `Score()` + `CanExecute()` + `CreateCommand()` (cast `caster` to `HostileCharacter`, not `GridElement`). Place the asset under `Assets/Data/Abilities/Enemy/`.
- New event channels: create a typed subclass of `GenericEventChannelSO<T>` and matching listener.
- Grid positions use the `Vector2Int` offset coordinate system (odd-row offset for hex).
- New scripts go under `Assets/Scripts/Runtime/` — anything outside it lands in a different assembly and will not be visible to the game.
- Tests for new systems go in `Assets/Tests/EditMode/`, named `<ClassUnderTest>Tests.cs`; hand the work to `@qa-engineer`.
- Code comments, XML doc comments, and Inspector strings (`[Tooltip]`, `[Header]`) are written in English; identifiers and USS names already are.
