# Minecraft Unity Edition

A Unity voxel sandbox focused on recreating Minecraft Beta 1.7.3 world generation, with an expanding survival game built around it.

This fork builds on [BossSloth/Minecraft-Unity](https://github.com/BossSloth/Minecraft-Unity). Development currently takes place on **`worldgen/layered-beta-terrain`** in [JoshtynR/Minecraft-Unity-Edition](https://github.com/JoshtynR/Minecraft-Unity-Edition).

The project combines Beta-style terrain and atmosphere with selected newer features, including hunger, armour, modern chest models, and configurable lighting. It is **not a complete or uniformly version-accurate Minecraft recreation**. Features and save formats are still evolving.

## Contents

- [Getting started](#getting-started)
- [Features](#features)
- [Controls](#controls)
- [Building](#building)
- [Worlds and saves](#worlds-and-saves)
- [Project structure](#project-structure)
- [Rendering and performance](#rendering-and-performance)
- [Textures, items, and audio](#textures-items-and-audio)
- [Validation](#validation)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Credits and rights](#credits-and-rights)

## Getting started

### Requirements

- **Unity 2022.3.18f1**, the version recorded in `ProjectSettings/ProjectVersion.txt`.
- Unity Hub and the build-support module for your target platform.
- Git, with enough disk space for Unity's imported assets and build cache.
- An internet connection for the initial Unity Package Manager restore. The package manifest includes a Git dependency.
- Optional: Python 3 and Mono (`mcs` and `mono`) for standalone regression checks.

### Open the project

```bash
git clone --branch worldgen/layered-beta-terrain https://github.com/JoshtynR/Minecraft-Unity-Edition.git
cd Minecraft-Unity-Edition
```

1. In Unity Hub, add the cloned repository directory—not its `Assets` directory.
2. Open it with Unity 2022.3.18f1 and allow the first import/package restore to finish.
3. Resolve any compilation errors before entering Play Mode.
4. Open `Assets/Scenes/MainMenu.unity` and press Play.
5. Create a world or select an existing save through the menu.

Start from **MainMenu** for the normal initialization flow. Opening the World scene directly is not a substitute for world selection and session setup.

### Steam and solo play

Steam identity/transport integration is present, with a local identity and KCP transport fallback when Steam is unavailable. Steam is not intended to be required for solo play.

Single-player still uses **Mirror's in-process host** for shared game authority. The local-only startup path sets `NetworkServer.dontListen`, so it does not need a public listening endpoint. Local player actions use direct host paths where implemented; remote clients retain networking and validation. This is not a separate, network-free game engine, and a Mirror server object in solo play does not by itself mean the game is hosting a public server.

## Features

These are implemented systems, not a claim that every edge case is complete or identical to vanilla.

### World generation and dimensions

- Seeded Beta 1.7.3-style terrain, biome sampling, surface composition, and caves.
- Population passes for trees, ores, plants, lakes, and other decorations.
- Biome-dependent grass and foliage colouring, including cold-biome snow.
- Overworld and Nether world generation, portal travel, dimension storage, and loading transitions.
- Spawn-area readiness checks and safe-spawn selection.
- Java-reference fixtures for generation stages and population features.

**Parity scope:** the generation tools compare specific seeds, coordinates, stage outputs, and feature fixtures against the reference. They do not certify the entire live game, every seed, or vanilla world-service timing. See [BetaParity](Tools/BetaParity/README.md) and [PopulationParity](Tools/PopulationParity/README.md).

### Blocks and world simulation

- Individual block textures rather than requiring atlas coordinates for standard faces.
- Special geometry and state handling for stairs, slabs, fences, torches, doors, trapdoors, beds, plants, and other non-cube blocks.
- Water/lava flow, fluid reactions, animated textures, and underwater/in-lava effects.
- Buckets that collect source water/lava and place source blocks; water evaporates in the Nether.
- Falling sand and gravel, support rules, leaf decay, grass spreading, crop states, and farming.
- TNT/explosions and block/item effects.
- Rideable boats with buoyancy and minecarts with rail traversal, saved per dimension; see [vehicle controls and current limits](Tools/VehicleChecks/README.md).
- Functional chests, double chests, furnaces, beds, and crafting interfaces.

### Player and survival

- Health, hunger, sprinting, eating, fall damage, and starvation.
- Survival mining, tool-dependent breaking, item drops, and durability.
- Crafting and smelting recipes.
- Armour equipment, damage reduction, wear, HUD indicators, and inventory-preview layers.
- Death, respawn, sleeping, and time advancement.
- First-person arm and held-item rendering, walking bob, and use animations.
- Inventory player preview and player skin support.

Armour preview support should not be confused with a guarantee that every third-person equipment model is complete.

### Inventory and containers

- Survival inventory, hotbar, stack counts, crafting, and equipment slots.
- Stack splitting, shift transfers, drag distribution, and same-item collection.
- A toggleable TooManyItems-style catalogue with search and source-ID-oriented ordering.
- Single and double chest storage, open/close animation, and separate half storage.
- Breaking a filled chest releases its contents; active container sessions remain protected.
- Furnace input, fuel, output, and smelting progress.
- Saved inventories rather than automatic recovery-item grants.

### Mobs, effects, and presentation

- Passive and hostile mobs, spawning rules, movement, combat, drops, and persistence.
- Mob-specific behaviours, breeding, hurt/death effects, and animations.
- Custom particle rendering and simulation for block debris, mining, smoke, portals, and other effects.
- Sky, sun/moon, sunsets, fog, and repeating extruded clouds.
- Smooth block lighting, ambient occlusion, and separate inventory-item lighting.
- Gameplay sounds and independent menu/world music playlists with volume controls.
- A dirt-background loading screen that remains until world/player readiness conditions are met.

## Controls

The project uses Unity's legacy Input Manager. Bindings below describe the current default code/input setup; custom settings and future changes may differ.

| Action | Default input |
| --- | --- |
| Move / look | WASD / mouse |
| Jump / swim upward | Space; holding jump repeats while grounded |
| Crouch / fly downward | Left Shift |
| Sprint | Double-tap W, subject to survival conditions |
| Break / attack | Left mouse button |
| Use / place / interact | Right mouse button |
| Inventory | E |
| Select hotbar slot | 1–9 / mouse wheel |
| Drop selected item | Q |
| Toggle TooManyItems catalogue | F1 while the inventory is open; initially hidden |
| Toggle flight | V |
| Pause / close menu / wake | Escape |

The flight toggle and item catalogue are development/convenience features, not evidence of strict Beta survival rules.

## Building

1. Open **File → Build Settings**.
2. Confirm these scenes are enabled, in this order:
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/World.unity`
3. Select your platform and install its Unity build-support module if needed.
4. For Windows desktop testing, use a **64-bit** target.
5. Build into a separate output directory, outside `Assets`.
6. Distribute the complete build output together, not just the executable.

Mono is a useful desktop baseline. IL2CPP can also be evaluated, but needs its platform C++ toolchain and separate compatibility/performance testing. Neither backend is a blanket solution to chunk-generation or memory issues.

For comparisons, use the same commit, seed, render distance, settings, and travel route. Measure chunk throughput, frame-time spikes, memory across dimension changes, and save/load correctness—not only peak FPS. Use Development builds for profiling and non-Development builds for representative release comparisons.

## Worlds and saves

World data is stored beneath the operating system's `Environment.SpecialFolder.ApplicationData` directory:

```text
.minecraftUnity/saves/
```

On a typical Windows installation this is:

```text
%APPDATA%\.minecraftUnity\saves\
```

Storage includes world metadata (`world.json`), chunk files, player data, and container/entity state. Dimension-specific storage is resolved by `DimensionRules`; do not manually merge dimension folders.

- Full-world autosaving is scheduled approximately every **two minutes**.
- Explicit saves also occur on relevant exit/disconnect and dimension-travel paths.
- Streaming dirty chunks can be written when unloaded; autosaving is not the only disk activity.
- Existing generated chunks are not regenerated when generator code changes.
- **Back up saves before updating the branch**, especially before testing storage or generation changes.
- Very old saves may lack the original `betaWorldSeed`. See the [generation compatibility notes](Tools/BetaParity/README.md) before extending them. The old spatial seed offset cannot recover the original long seed.

## Project structure

| Location | Responsibility |
| --- | --- |
| `Assets/Scenes` | Main menu and world scenes |
| `Assets/_Scripts/WorldGeneration` | Terrain, biome, noise, and population generation |
| `Assets/_Scripts/World` | Chunk storage, simulation, lighting, saving, and world lifecycle |
| `Assets/_Scripts/World/Rendering` | Chunk meshes, special geometry, sky, particles, mobs, and item visuals |
| `Assets/_Scripts/Block` | Block types, access, collision, and related rules |
| `Assets/_Scripts/Entity's/Player` | Player movement, survival, inventory, interactions, and UI |
| `Assets/_Scripts/Networking` | Session startup, authority, and transport integration |
| `Assets/_Scripts/Menus` | World selection, creation, settings, and menu flow |
| `Assets/Editor` | Asset inspectors, texture assignment, and editor utilities |
| `Assets/Resources` | Runtime-loaded data, materials, prefabs, and sounds |
| `Assets/Textures/Minecraft` | Block, item, GUI, entity, and environment textures |
| `Tools` | Standalone checks, fixtures, and reference comparison tools |

Many large systems use partial classes. For example, `World_*.cs` and `Player_*.cs` divide responsibilities without creating separate world/player instances. Follow the owning subsystem rather than putting all new behaviour into the central class file.

## Rendering and performance

The active architecture includes:

- **16×16×16 render sections** with section-level dirty tracking instead of rebuilding a whole column for every edit.
- Distance/visibility-aware rebuild prioritization.
- Cached neighbouring chunk access during meshing.
- Direct face-normal generation for standard geometry, with special-model handling where required.
- A **56-byte packed chunk vertex** layout; this is smaller than the previous layout, not a claim to use Beta's exact vertex format.
- Packed section cells (`ushort`: block type plus state) and byte-packed sky/block light.
- Parallel chunk-local generation and mesh preparation, with Unity mesh application on the main thread.
- Worker lifetime/cancellation tracking and upload ownership checks during unloads and dimension changes.
- Shared spatial indexing for nearby mob/item queries.

Population writes retain controlled ordering because decorations can modify neighbouring chunks. Moving every pass into unrestricted parallel work would risk changing terrain or introducing races.

Rendering, chunk data, simulation, and save ownership are separate concerns. Changes to unload/reset logic must account for workers still holding chunk data. A cancelled result must not be uploaded into a replacement world.

Enable `World.logGenerationTimings` in the Inspector for generation-stage diagnostics. Profile travel into new terrain and repeated dimension changes as well as standing in an already-loaded area.

## Textures, items, and audio

### Block and item definitions

- `BlockDataSO` and its editor hold block definitions and texture references.
- `ItemDataSO` holds item definitions; the runtime asset is `Assets/Resources/Item Data.asset`.
- Block IDs and inventory item IDs have distinct responsibilities. Use the existing conversion/rule helpers rather than assuming every item is a placeable block.
- Standard blocks support top, bottom, and side images. Special models may require additional face mappings, animation settings, or geometry-specific textures.
- Preserve existing asset GUIDs and `.meta` files when updating assignments. Recreating a data asset can sever scene/prefab references.

### Texture assignment utilities

The editor includes:

- **Minecraft → Textures → Assign From Minecraft Folder**
- **Minecraft → Textures → Bind Missing Fluid Textures**
- **Minecraft → Textures → Open Assignment Report**

Review the assignment report and Inspector results. A matching filename does not guarantee that a texture uses the expected model layout. Modern single/double chest entity skins, Beta bed face textures, GUI sheets, and vertical fluid animation strips are not interchangeable formats.

### Audio

Gameplay sounds live under `Assets/Resources/Sounds`. `MusicManager` has separate menu and world track collections and scene-driven playlist selection. Keep tracks in the intended pool rather than relying on a single playlist for both scenes.

## Validation

Standalone checks compile selected production C# sources against lightweight Unity/network doubles. They provide fast regression coverage without opening the Editor, but are **not a full Unity build or Play Mode test**.

From the repository root, with Python 3 and Mono installed:

```bash
python3 Tools/BetaParity/run.py
python3 Tools/PopulationParity/run.py
python3 Tools/CreativeChecks/run.py --custom-assets
python3 Tools/StartupChecks/run.py
python3 Tools/NetherChecks/run.py
python3 Tools/MobChecks/run.py
python3 Tools/BucketChecks/run.py
python3 Tools/SleepChecks/run.py
```

Check each runner's `--help` and local README for dependencies/options. Several accept explicit compiler/runtime paths, for example:

```bash
python3 Tools/BucketChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
```

| Suite | Main focus |
| --- | --- |
| [BetaParity](Tools/BetaParity/README.md), [PopulationParity](Tools/PopulationParity/README.md), [TreeParity](Tools/TreeParity/README.md) | Generation stages, RNG, and feature fixtures |
| [CreativeChecks](Tools/CreativeChecks/README.md) | Inventory, recipes, containers, equipment, and geometry regressions |
| [ChunkRenderingChecks](Tools/ChunkRenderingChecks/README.md) | Rebuild scheduling, invalidation, uploads, and worker ownership |
| [StartupChecks](Tools/StartupChecks/README.md) | Loading, music, identity, and startup integration |
| [NetherChecks](Tools/NetherChecks/README.md), [OfflineChecks](Tools/OfflineChecks/README.md) | Dimensions and local-host interaction paths |
| [MobChecks](Tools/MobChecks/README.md) | Mob rules and runtime integration |
| `BucketChecks`, `SleepChecks` | Source-fluid interactions and sleep clock completion |

Other focused suites cover lighting, fluids, audio, HUD, shapes, mining, spawning, and memory-related invariants. Some older isolated harnesses have incomplete mocks or known failing assertions; their existence is not an all-green CI claim. Consult their output and distinguish a harness failure from a demonstrated game regression.

Before publishing a build, manually test world creation, reload, respawn, Nether round trips, inventory/container transactions, equipment, buckets, sleeping, and returning to the menu. Check real audio, shaders, UVs, particle appearance, and multiplayer separately: mocks cannot establish visual correctness or transport behaviour.

## Troubleshooting

### Compilation errors after pulling

Let Unity finish importing, then address the first actual compiler error. Check that all files from the commit—including `.meta` files—were pulled. Do not work around missing gameplay code by simply removing its input call.

If the compiler reports merge markers, resolve `<<<<<<<`, `=======`, and `>>>>>>>` in the affected file before testing. Preserve local work before pulling; do not reset an entire worktree just to make an update apply.

### Loading does not complete

Capture the first exception and the loading stage. Player initialization, spawn data, and rendered spawn chunks must all be ready; hiding the loading screen does not fix the underlying failure. Start from MainMenu and check for duplicate session managers or stale sessions from an earlier run.

### Network warnings in solo play

An in-process host is expected; repeated `NetworkClient is not ready` warnings from normal local interactions are not. Report the triggering action and stack trace. Do not fix this by globally waiting on remote readiness or disabling movement: local and remote authority paths need different handling.

### Missing or inconsistent textures

Check the assigned data asset, material shader, texture import settings, and assignment report. Confirm that special-model textures match their expected format. Test a newly placed block as well as an existing saved one when investigating state/orientation issues.

### Memory stays high after Nether travel

Compare repeated round trips and returns to the menu with the Unity Profiler/Memory Profiler. Record loaded chunks, mesh/native resources, and managed allocations. Process working-set size alone does not identify a leak, and replacing Mono with IL2CPP is not a diagnosis.

## Contributing

- Branch from the current development branch and keep unrelated changes separate.
- Preserve serialized references, stable IDs, and existing save compatibility where possible.
- Keep gameplay authority explicit: local host actions must not wait on remote network readiness; remote requests still need validation.
- Keep Unity object creation/upload on the appropriate thread and respect world-worker lifetimes.
- For parity changes, include the seed, coordinates, stage, expected reference result, and a regression fixture.
- For rendering changes, include before/after screenshots with the same time, biome, weather/settings, and camera where possible.
- For performance changes, include a reproducible route and measurements rather than only an FPS claim.
- Run the relevant focused checks and describe what still needs testing in Unity.
- Do not commit generated `Library`, `Temp`, or build-output directories, personal saves, credentials, or machine-specific paths.

## Credits and rights

- Original Unity project: [BossSloth/Minecraft-Unity](https://github.com/BossSloth/Minecraft-Unity).
- Beta reference used for comparison: [jacobo-mc/mc_b1.7.3_release](https://github.com/jacobo-mc/mc_b1.7.3_release).
- Third-party components include Mirror, KCP, FizzySteam/Facepunch integration, UniTask, and Unity packages. Retain and review their bundled notices and licenses.

This is an unofficial fan project, not affiliated with or endorsed by Mojang or Microsoft. Minecraft names and original game assets belong to their respective owners. Inclusion in this repository does not grant redistribution rights to textures, sounds, music, reference code, or other third-party content. Review permissions before distributing a source package or build.

No new project-wide license is granted by this README; check applicable repository and dependency notices before reuse.
