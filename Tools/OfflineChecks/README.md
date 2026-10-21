# Steam and offline Windows identity

Run `python Tools/OfflineChecks/run.py --mcs <compiler> --mono <runtime>`.
The complete production identity service and MinecraftNetworkManager compile
against small Unity/Mirror/Steam doubles. Checks cover unavailable native Steam,
failed identity getters, cached connection identity, reconnecting with Steam,
OS-user preferences, patch-save compatibility, host transport selection and
restoring Steam transport for the next connection. Native Steam/transport and
Unity player execution still require a real build.

Steam sessions retain the real Steam ID and display name. With Steam unavailable,
single-player uses Environment.UserName and an offline ID in PlayerPrefs scoped
to the Windows user. The initial offline ID is 1 to retain the temporary DLL
patch's playerdata/1.json saves. Steam and offline profiles keep separate player
inventories; no account save files are renamed or merged. Connection identity is
captured until disconnect so Steam becoming available/disappearing cannot change
the current save target.

The prefab's Steam transport remains configured. Offline hosts select the existing
KCP transport before listening (or add it for an older scene), while a subsequent
Steam session restores Steam transport. Steam lobby listing/creation/joining still
requires Steam. Fresh installs create the save root; incomplete folders without
world.json are excluded from the world list.

For player validation, build normally and launch on a PC without Steam. Create,
save and reopen a world; check the Windows player name and restored inventory.
Repeat with Steam running: check Steam name, Steam-ID saves and multiplayer.
Also test cancel/rejoin and starting the World scene directly in the editor.

## World startup ordering

The harness compiles production identity and network-manager code against Unity,
Steam and Mirror test doubles. It checks offline transport selection, stable
account identity, and the startup coroutine when the client connects before the
world scene and message handlers are ready. It also covers the scene callback
initiating a request, duplicate callbacks, cancellation while waiting, and a
remote client without a local server. Source checks verify that world/server
readiness is published after registering the startup message handlers.

This does not run Unity's actual scene loading or network transport. Confirm
Create World and reopening a save in the editor and a standalone build.
