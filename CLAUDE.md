# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workflow

Before implementing any change that requires more than a single edit (anything beyond a label, rename, or trivial fix), enter plan mode and show the plan for approval before touching any file.

During planning, if there are any implementation doubts — design decisions, ambiguous requirements, trade-offs between approaches — ask the user before finalising the plan. Never make large assumptions; always have all necessary information before exiting plan mode.

## Project Overview

**Silly Pirates** is a tactical turn-based combat game built in Unity (URP). Characters occupy a hexagonal grid, take turns spending action points and movement points on abilities, and interact through a ScriptableObject-driven event architecture.

## Unity & Build

This is a Unity project — there is no CLI build command for day-to-day development. Open the project in the Unity Editor and use the standard Play/Build workflow. The Unity MCP integration (`unity-mcp` package) allows Claude Code to interact with the running Editor directly via MCP tools.

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

## Key Packages

| Package | Purpose |
|---|---|
| PrimeTween (local plugin) | Tween animations (movement, UI) |
| Cinemachine v3 | Camera management |
| Input System v1.19 | New input system |
| URP v17 | Rendering pipeline |
| Behavior Tree v1.0 | Enemy AI |
| unity-mcp (git) | MCP ↔ Unity Editor bridge |

## Specialized Agents

Project-specific agents are in `.claude/agents/` — invoke with `@<name>` in the chat:

| Agent | When to use |
|---|---|
| `@performance-checker` | Review code that runs in Update/LateUpdate or is called frequently — catches GC allocations, closure captures, expensive per-frame calls |
| `@ui-builder` | Build new HUD or world-space UI elements, including from Figma — knows the UXML/USS/UxmlElement patterns and PrimeTween conventions |
| `@system-builder` | Design and implement a new game system — enforces SO-first data, event channels, `Awaitable` async, and interface contracts |
| `@ability-designer` | Create new combat abilities — knows the `AbilityBase` three-method contract, shape system, command pattern, and asset wiring |
| `@sound-implementer` | Wire SFX, ambience and music into gameplay — knows `AudioDirector`, `SoundEventSO`, the three cue channels and the pooling rules. Implements sounds, never creates them |
| `@camera-director` | Decide/tune how abilities are framed on screen — owns the camera cue system (`CameraCueType`, `CameraCueProfileSO`, `CameraDirector`), assigns cues to ability assets, extends it (FrameArea, shake, FollowProjectile) |

## Coding Conventions

- Async operations use Unity's `Awaitable` (not `Task`) with `CancellationToken` propagation.
- ScriptableObjects are the primary data/configuration container; avoid duplicating state in MonoBehaviours.
- New player abilities: extend `AbilityBase` and place the asset under `Assets/Data/Abilities/`.
- New enemy abilities: extend `EnemyAbilityBase`, implement `Score()` + `CanExecute()` + `CreateCommand()` (cast `caster` to `HostileCharacter`, not `GridElement`). Place the asset under `Assets/Data/Abilities/Enemy/`.
- New event channels: create a typed subclass of `GenericEventChannelSO<T>` and matching listener.
- Grid positions use the `Vector2Int` offset coordinate system (odd-row offset for hex).
