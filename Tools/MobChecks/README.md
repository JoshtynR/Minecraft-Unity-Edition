# First hostile-mob batch

Run `python3 Tools/MobChecks/run.py --mcs <compiler> --mono <runtime>`.
The suite compiles the production hostile AI/projectile/damage module and shared
mob state against controlled Unity/Mirror/world-query doubles. It exercises melee
damage/cooldowns, sunlight ignition, bright-day spider neutrality and retaliation,
flying-player immunity, thirty-tick creeper fuses/defusing, and physical skeleton
arrow sweep hits, lifetime and visual cleanup. CreativeChecks additionally tests
species health, non-breedability, save identity and skin UV ranges.

World integration:
- Existing cow messages/save arrays carry the four appended species. Old enum
  values and saved adult cows remain unchanged. Materials use existing textures.
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
  have a 128-object cap, expire after 200 ticks, and are cleared with the world.

Unity smoke tests still required: daylight/night/cave spawning; inspect each
model and bow; save/reload all species; verify wall occlusion, climbing, fuse
flashes, explosion edits, projectile collision and two-client replication.

Limitations: steering/jumping is not full voxel pathfinding. Arrows currently use
a simple shaft visual and disappear on impact (no embedded-arrow pickup/save).
No XP, spawner blocks, spider jockeys, charged creepers, skeleton-kill discs,
advanced combat animations, fire overlays, or armor absorption is added here.
These are first playable implementations, not a claim of complete Beta parity.
