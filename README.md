# Silly Pirates

A tactical, turn-based pirate combat game built in Unity (URP), played on a hexagonal grid — think small-scale skirmishes where crews spend action points and movement to line up abilities against each other.

> 🚧 **Actively in development — not yet playable end-to-end.** Core systems (turns, combat, abilities, enemy AI, grid) are functional and exercised in test scenes, but there is no complete playable loop yet, and a portion of the art is temporary placeholder.

## What this project demonstrates today

- **Turn-based combat loop** — a queue-driven turn system built on Unity's `Awaitable` async model, where each turn agent spends action points, movement points, and agility.
- **Combat state machine** — a three-state FSM (`Idle → Targeting → Execution`) implemented with cloneable ScriptableObject states, coordinating ability targeting and execution.
- **Data-driven ability system** — abilities are ScriptableObjects with a clean three-method contract (preview affected cells, build an executable command, validate readiness), with pluggable AoE shapes (circle, line) decoupled from ability logic.
- **Enemy AI** — a `com.unity.behavior` Behavior Tree drives enemy turns, scoring every available ability against the current game state and picking the best target automatically.
- **Hexagonal grid & pathfinding** — custom A* pathfinding over an odd-row offset hex grid, with occupancy tracking and reachable-area calculation.
- **Event-channel architecture** — systems communicate through typed ScriptableObject event channels instead of direct references, keeping combat, UI, camera, and input loosely coupled.
- **Command pattern with undo** — all state-mutating actions during a turn flow through queued, undoable commands.
- **Camera direction system** — a cue-based camera director that frames ability execution (zoom, follow, shake) based on per-ability profiles.
- **UI Toolkit HUD** — a radial ability menu and HUD elements (action points, turn order, crew overview) built with UI Toolkit and animated with PrimeTween.
- **8-directional sprite animation** — camera-relative directional sprite rendering for 2D characters on a 3D board.

## Tech stack

| | |
|---|---|
| Engine | Unity 6 (`6000.3.16f1`), Universal Render Pipeline 17 |
| Language | C# |
| Camera | Cinemachine 3 |
| Input | Unity Input System 1.19 |
| AI | Unity Behavior (Behavior Trees) 1.0 |
| Tweening | PrimeTween |
| UI | UI Toolkit (UIElements) |
| Tooling | MCP for Unity (Editor automation via Claude Code) |

## Project structure

```
Assets/
├── Scripts/Runtime/       # Gameplay code: Combat, Grid, TurnManagment, Ship,
│                          # ShipEquipment, Character, Input, Interactables, UI, Camera...
├── Scripts/Editor/        # Editor tooling
├── Data/                  # ScriptableObject configuration: Abilities, Characters,
│                          # Combat (states, turn system), Events (channels), Grid, UI
├── Prefabs/                # Character, ability, ship, and UI prefabs
├── Imports/                # 2D/3D art assets (mix of final and placeholder — see note below)
└── Plugins/                 # Third-party packages included in source (e.g. PrimeTween)
```

The architecture favors **ScriptableObjects as the primary data/config layer**, **event channels** for loose coupling between systems, and the **command pattern** as the single path for mutating game state during a turn. See [`CLAUDE.md`](./CLAUDE.md) for the full architecture reference used to guide AI-assisted development on this repo.

> **Note on art assets:** a portion of the 2D sprites/icons under `Assets/Imports` are temporary placeholders sourced externally during prototyping and are scheduled to be replaced with original art; not all of their licenses have been individually verified. A couple of Unity Asset Store packages used only for local prototyping (a free skybox pack and a nature asset kit) are excluded from this repository via `.gitignore` and are not part of the published source.

### Getting the project fully working after cloning

Two free Unity Asset Store packages are used for prototyping but excluded from this repo (their EULA doesn't allow redistributing the source files). To get scenes referencing them to resolve correctly, download and import them manually from the Asset Store, then place the extracted contents at the exact paths below:

| Package | Import path |
|---|---|
| Fantasy Skybox FREE | `Assets/Fantasy Skybox FREE/` |
| Proxy Games — Stylized Nature Kit Lite | `Assets/Proxy Games/` |

Without them, the project still opens and the code compiles — you'll just see missing-material/pink-shader placeholders on the skybox and a few nature props in the affected scenes.

## License

This project is released under the **[PolyForm Noncommercial License 1.0.0](./LICENSE)**.

🇮🇹 **In breve:** puoi leggere, studiare e imparare da questo codice liberamente, ma **non puoi usarlo per scopi commerciali** senza permesso, e se lo riprendi devi citare la fonte originale.

🇬🇧 **In short:** you're free to read, study, and learn from this code, but you **may not use it for commercial purposes** without permission, and any reuse must credit the original source.

## Screenshots / Preview

![In game screenshot](/Screenshots/in_game_screenshot.png?raw=true "Screenshot in game")

## Contacts

### Lorenzo Camanzi
- **[Github Profile](https://github.com/camanzi)**
- **[Itch.io](https://lorenzo-camanzi.itch.io/)**
- **[Linkedin](www.linkedin.com/in/lorenzocamanzi)**
>
- Email: lorenzo.camanzi@gmail.com
- Cell:  +39 331 107 0892