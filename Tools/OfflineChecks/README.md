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
