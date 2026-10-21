Run `python Tools/StartupChecks/run.py --mcs <compiler> --mono <runtime>`.

Compiles the production music manager and persistent loading screen against Unity test doubles. Checks end-of-track transitions, completed/interrupted/paused fades, playlist wrapping, shuffled passes, one-shot playlists, null entries, single-track loops, listener pause, and stop/resume. Exercises scene activation, delayed player initialization/save restoration, missing spawn meshes, a stable-ready grace period, cancel, disconnect, and headless startup. Also checks the player/menu/network integration points.

Unity Play Mode is still needed to verify actual audio output, transport callbacks, scene rendering, and loading-screen appearance.
