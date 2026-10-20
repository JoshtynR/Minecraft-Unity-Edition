# Bare-hand survival mining

Reference reconstructed Beta 1.7.3 client commit
740c583901e1ff1150e9ef37e37dab5bc0e4f807:
https://github.com/jacobo-mc/mc_b1.7.3_release
Block.java (hardness/blockStrength), Material.java/InventoryPlayer.java (hand
harvest), EntityPlayer.java (water/air penalties), BlockStep.java, BlockIce.java,
BlockLeaves.java, BlockGravel.java and PlayerControllerSP.java.

| Current block | Hardness | Nominal bare-hand seconds | Existing block drop |
| --- | ---: | ---: | --- |
| Dirt / sand | 0.5 | 0.75 | Same block |
| Grass | 0.6 | 0.9 | Dirt |
| Gravel | 0.6 | 0.9 | Gravel unless flint roll |
| Oak / spruce / birch logs | 2 | 3 | Same log |
| All leaves | 0.2 | 0.3 | No leaf block |
| Glass | 0.3 | 0.45 | None |
| Ice | 0.5 | 0.75 | None; becomes water when supported |
| Stone | 1.5 | 7.5 | None without a tool |
| Sandstone | 0.8 | 4 | None without a tool |
| Wooden slab | 2 | 10 | None without a tool |
| Glowstone | 0.3 | 1.5 | None without a tool |
| Bedrock / fluids | Unbreakable | — | None |

These are nominal duration values; repeated Java-style float damage at 20 ticks/s
can cross the completion threshold one tick later. No held block grants tool
speed. Beta's wooden slabs/glowstone use rock material; later Java rules differ.
Harvestable-by-hand blocks divide strength by 5 underwater and again by 5 while
airborne. In Beta, the unharvestable branch uses 1/hardness/100 and does not use
those strength penalties. Water/lava are skipped when aiming; slabs use actual
shape bounds. Targeting can now select ice even though its existing block data
currently disables its collider.

Holding left mouse builds progress; release, opening a menu, changing target,
health zero, moving out of reach, obstruction or target changes reset/cancel it.
The server advances its own 20Hz progress, validates type/reach and the provided
bounded hit point's line of sight, replaces the block only on completion, and
notifies the owner once. Five-tick (0.25s) post-break delay applies. The old raw
SetBlockMessage can no longer remove blocks instantly. Placement still uses its
existing path. Successful hand harvesting adds 0.025 exhaustion to the newer
hunger system; this is a survival addition beyond Beta. Host/offline and remote
players use separate visual/authoritative progress. Network latency can produce
short visual differences; aim/movement validation is not a full anti-cheat.

Drops now use the world-item system: mining spawns pickup entities rather than
inserting directly into inventory. Grass gives dirt; gravel can drop flint and
leaves can drop their species' sapling. Existing hand-harvest restrictions remain.
Ice conversion to water uses the existing water simulation. Tool mining speeds
and harvest tiers are not introduced by the durability update.

Completed mining consumes held-tool durability using the Beta costs in
`ItemDurability`: axes/shovels/pickaxes one, swords two, shears one on leaves/webs.
Canceled/failed breaks consume none. Changing item identity/damage resets mining.
The server snapshots the selected tool at mining start and validates it before
completion. Breakage happens after the final block completes. Host receipts do
not damage a second matching tool; remote wear revisions reject older incoming
inventory snapshots from pickup/chest/mining interactions.

The editor auto-binds the user's `Assets/Textures/Minecraft/block_break.png` to
Resources/PlayerHudAssets.asset, preserving existing HUD bindings. That PNG is
absent from the remote branch when implementing this change. The overlay uses
an owned reusable 24-vertex mesh shaped to the block and a material clone, both
destroyed on disable/destruction. Its multiply shader preserves transparent and
white-background pixels and darkens the existing lit block under the cracks.
Automatic layout detection supports ten square frames in horizontal/vertical
strips or 5x2/2x5 grids, read left-to-right then top-to-bottom. Other grids can set
Break Columns/Rows on the config asset. The actual PNG has not been inspected,
so frame order/artwork and GPU appearance require a Unity play test.

Run `python Tools/MiningChecks/run.py --mcs <compiler> --mono <runtime>`.
The harness compiles production rules, Player_Mining and BlockBreakOverlay
against Unity/Mirror doubles. Checks cover current-block hardness/harvest
penalties, progress before/completion, single drops, cancellation, out-of-reach
and blocked targets, changed blocks, cooldown, bedrock, supported ice, strip
UVs and mesh/material cleanup. These checks do not compile the whole Unity
project, run shader compilation, texture importing, Mirror weaving or live RPC
transport. Test holding/releasing and target switching, mining half slabs,
standing underwater/in air, and host+remote clients in Unity.
