# Shared mob entity system

`MobEntitySystem` owns persistent entities, transient navigation/targets and a
bounded 20 Hz clock. `MobDefinition` holds species dimensions, health, movement,
material and sounds. `MobBrain` dispatches species behavior; cows are one entry.
`World_Mobs` provides authority, physics, interactions and sync. `World_MobAI`
handles farm behavior/navigation; `World_HostileMobs` handles combat/projectiles.
`MobVisual`, `MobModelGeometry` and `MobAnimation` handle presentation.

Navigation is bounded to 192 cells per search and two searches per tick with
rotating entity processing. Separation uses bounded spatial buckets. Unloaded
entities release transient runtimes and renderers; teardown releases shared
meshes. The independent mob random stream never consumes terrain RNG.

New saves use `mobs.entities`; old `cows.animals` remains readable with stable
species values, IDs and random state. Inspector renames retain serialization
aliases and script/resource GUIDs. All multiplayer peers need this version
because network message types changed.

World-owned stores use thread-safe lazy initialization and nonserialized backing
fields. Save restoration and repeated resets are tested with field initializers
deliberately bypassed, covering scene-loaded callbacks before Start and missing
runtime caches after script reload. WaterChecks also verifies concurrent first
use of the water notification queue.

Run `python3 Tools/MobChecks/run.py --mcs <compiler> --mono <runtime>`.
The suite compiles the production hostile AI/projectile/damage module and shared
mob state against controlled Unity/Mirror/world-query doubles. It exercises melee
damage/cooldowns, sunlight ignition, bright-day spider neutrality and retaliation,
flying-player immunity, thirty-tick creeper fuses/defusing, and physical skeleton
arrow sweep hits, lifetime and visual cleanup. A second integration pass compiles
the actual world adapter, renderer and player interactions to test food following,
sixty-tick courtship, old save restoration, full-block jumps, species clearance,
render mesh teardown and snapshot batching. CreativeChecks additionally tests
species health, non-breedability, save identity and skin UV ranges.

World integration:
- Generic mob messages/save arrays carry all eight species. Old enum
  values and saved adult mobs remain unchanged. Materials use existing textures.
- Natural spawning is Overworld-only, in loaded supported dark air cells, at
  least 24 blocks from players, with a separate configurable hostile cap.
- Zombies use melee; skeletons shoot every 30 ticks; both ignite in exposed
  daylight. Spiders are neutral in bright light unless recently hurt, and climb
  when blocked. Creepers start fusing within three blocks, defuse on lost sight
  or range, and explode at thirty ticks with resistance-aware block rays.
- Rotten flesh, arrows/bones, gunpowder and string use bounded drop rolls. Creeper
  explosions respect the project's protected non-empty/open chests. Ordinary
  destructible blocks have a chance to drop their harvest loot.
- Server/offline authority owns damage, arrows, spawning and explosions. Arrows
  have a 128-object cap, fly for at most 200 ticks, lodge in blocks for up to
  1200 ticks and fall when their support disappears. They clear with the world.
- Farm animals follow food, babies follow adults, sheep graze and chickens lay
  eggs. Rendering includes eight spider legs, leg fleece, head tracking, chicken
  wings, modern pig snouts on supported skins, breeding hearts and fire overlays.

Unity smoke tests still required: daylight/night/cave spawning; inspect each
model and bow; save/reload all species; verify wall occlusion, climbing, fuse
flashes, explosion edits, projectile collision and two-client replication.

Limitations: navigation is bounded rather than exhaustive. Projectiles are not
saved across world reloads. Pig riding, XP, spawners, spider jockeys, charged
creepers and player armor absorption remain unfinished. Requested breeding/meat
features remain a hybrid with Beta behavior, not a claim of complete parity.
Actual Unity shaders/rendering and live Mirror transport require play-testing.
