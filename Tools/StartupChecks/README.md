Run `python Tools/StartupChecks/run.py --mcs <compiler> --mono <runtime>`.

Compiles the production music manager and persistent loading screen against Unity test doubles. Checks end-of-track transitions, completed/interrupted/paused fades, playlist wrapping, shuffled passes, one-shot playlists, null entries, single-track loops, listener pause, and stop/resume. Exercises scene activation, delayed player initialization/save restoration, missing spawn meshes, a stable-ready grace period, cancel, disconnect, and headless startup. Also checks the player/menu/network integration points.

Unity Play Mode is still needed to verify actual audio output, transport callbacks, scene rendering, and loading-screen appearance.

The runner also checks initial metadata creation against real temporary files:
no chunk snapshots before spawning, preservation of existing metadata, dirty-state
restoration after failed IO, and edits made while a save is writing. Creating a
new world now writes its small seed/list entry first; chunks remain dirty for the
normal autosave/exit save. Initial joins skip portal departure/arrival saves.

Host readiness checks in `Tools/NetherChecks` verify that initial joins reuse
prepared terrain, do not serialize snapshots for the shared local client, and
still reconcile/persist player inventory. Real portal travel retains checkpoint
saves. Ready-area checks require populated renderers plus the raw-data halo.
Enable `World.logGenerationTimings` in the inspector for stage timings in Unity;
`[WorldStartup]` logs the initial player handshake duration. These wall-clock
diagnostics are intended for real-player profiling, not an FPS guarantee.

Loading UI checks cover opaque dirt rendering, native tile size and GUI scaling, progress clamping/reset, native cancel-button rows, and suppression of the old isometric camera without disabling the UI camera. The World scene also disables the preview camera before its first frame. Generation reports milestones and applied-chunk progress; the readiness gate still waits for player data and the spawn mesh.
