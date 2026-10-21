Run `python Tools/StartupChecks/run.py --mcs <compiler> --mono <runtime>`.

Compiles the production music manager and persistent loading screen against Unity test doubles. Checks end-of-track transitions, completed/interrupted/paused fades, playlist wrapping, shuffled passes, one-shot playlists, null entries, single-track loops, listener pause, and stop/resume. Exercises scene activation, delayed player initialization/save restoration, missing spawn meshes, a stable-ready grace period, cancel, disconnect, and headless startup. Also checks the player/menu/network integration points.

Unity Play Mode is still needed to verify actual audio output, transport callbacks, scene rendering, and loading-screen appearance.

Loading UI checks cover opaque dirt rendering, native tile size and GUI scaling, progress clamping/reset, native cancel-button rows, and suppression of the old isometric camera without disabling the UI camera. The World scene also disables the preview camera before its first frame. Generation reports milestones and applied-chunk progress; the readiness gate still waits for player data and the spawn mesh.
