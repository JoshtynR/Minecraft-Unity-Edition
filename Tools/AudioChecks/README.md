Run `python Tools/AudioChecks/run.py --mcs <compiler> --mono <runtime>`.

Compiles the production sound resolver, material rules, audio pool, and player observer against Unity doubles. Checks numbered sound variants, cached/missing lookups, `step 1` fallbacks, block materials, door states, overlapping positional voices, bounded allocation, loading/headless guards, and player footsteps/hurt/level-up events. Verifies all 35 mapped sound families exist in `Assets/Resources/Sounds`.

The creative and mining integration checks also verify chest replication does not duplicate sounds and only completed mining broadcasts a break sound. `CreativeChecks/run.py --custom-assets` preserves and validates user-edited item values instead of requiring original Beta stack sizes.

Runtime uses existing files directly through Resources. Canonical `step` clips take precedence; missing material footsteps fall back to `step 1`, then `dig`. Existing explicitly assigned fluid/pickup clips retain priority. Only needed sound directories are loaded and cached. Missing clips are silent. Playback uses 24 scene-owned sources, with no per-sound GameObjects and no changes to generation RNG.

Unity Play Mode is still required to verify actual audibility, import settings, spatial balance, and Mirror RPC delivery with two clients. Sounds for systems that do not yet exist (other mobs, weather, minecarts, fireworks, and so on) are not triggered.
