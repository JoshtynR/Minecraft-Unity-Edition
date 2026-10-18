Modern single chests
===================

Right-click a chest without crouching to open 27 storage slots above the existing
36 player slots. Left/right click moves stacks, right click splits or places one,
and shift-click transfers between chest and player. E/Escape closes it. The
creative catalogue is hidden while a chest is open. Adjacent single chests remain
independent; large/double chests and their textures are not implemented here.
LockedChest retains its old rendering and has no container interaction.

PlayerHudAssetSetup automatically binds these existing user images on editor
reload/import (Minecraft > Textures > Refresh GUI and Chest Assets also binds):

- Assets/Textures/Minecraft/block/normal.png (64x64 modern chest skin in this repo)
- Assets/Textures/Minecraft/entity/chest/normal.png (preferred if provided)
- Assets/Textures/Minecraft/gui/container/generic_54.png (256x256 chest GUI)

Resources/ModernChestMaterial keeps the voxel chest shader included in builds.
Restart Play Mode after changing the chest assets. Missing skin/material preserves
the old closed block mesh and logs a warning; the container still works. Existing
Block Data.asset and item links are not rewritten by this feature.

ChestModelGeometry creates the 14x10x14 body, 14x5x14 lid and 2x4x1 latch with the
64x64 box unwrap. The lid/latch pivot around the rear edge and ease through 90
degrees over 0.5 seconds. Native facing metadata rotates the model. Chunk meshes
record chest positions and omit the old chest cube when modern assets are ready;
ChunkRenderer creates/reuses/removes separate animated models with chunk lifetime.
The selection/collision bounds are 1/16..15/16 wide and 14/16 high. Nearby block
faces are exposed around the inset body. Model lighting uses nearby voxel light;
it is flat per face, not terrain corner AO. Owned meshes/materials are destroyed.
Inventory block icons retain their existing block-face rendering.

The server checks chest type, distance, opaque line of sight, lid obstruction,
session ownership and transaction sequence. One player can use a chest at a time.
Click packets cannot submit chest contents or replace the in-session player stacks.
Opening seeds the session from the existing client inventory, matching this game's
current inventory authority; this does not add a general anti-cheat inventory system.
Clients wait for each reply and queue closing until in-flight transfers finish.
Remote death, lost reach and disconnect release the session and close the lid.
Lid changes are broadcast; a client joining mid-session sees the closed default
until the next lid event. There are no added chest sounds in this pass.

Named worlds save chest contents in chests.json alongside world.json, independently
of chunk streaming. Old worlds start with empty containers. Normal world save and
chest close save the contents; a .bak retains the previous sidecar file. Chest and
player snapshots are taken before async chunk IO and chest transfers pause while
the save is in flight. This is not a crash-atomic multi-file save system. Empty
chests can be mined. Filled or actively opened chests are protected from removal
because dropped-item entities are not implemented; empty the chest first.

CreativeChecks compiles production container/UI/network-handler/model code against
Unity/Mirror doubles and exercises random transfers, conservation, stack bounds,
replay/exclusive locks, save/reload, blocked lids, reach/LOS/death, in-flight close,
actual model animation progression and owned resource cleanup. JsonUtility is
mocked at its serialization boundary. ShapeChecks, BlockTextures, MiningChecks
and HudChecks cover existing behavior and the new binding/removal checks. Actual
Unity shader rendering, JsonUtility serialization and Mirror weaving must be
validated in Play Mode, including a remote-client transfer/reconnect test.
