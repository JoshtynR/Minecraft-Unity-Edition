# Sprint exhaustion and starvation

Hunger is a post-Beta-1.7.3 addition. This implementation uses the classic
pre-combat Java rules reconstructed in MCP 9.19 (Java 1.8.9):
https://github.com/Marcelektro/MCP-919/blob/main/src/minecraft/net/minecraft/util/FoodStats.java
https://github.com/Marcelektro/MCP-919/blob/main/src/minecraft/net/minecraft/entity/player/EntityPlayer.java

Food starts at 20 with hidden saturation 5. Ground walking/crouching costs 0.01
exhaustion per block; ground sprinting uses the reference float 0.099999994
(approximately 0.1) per block. Distance rounds to centimeters. Normal jumps
cost 0.2; sprint jumps cost 0.8. Flight is exempt. Exhaustion caps at 40; strictly
above 4 it consumes 4 and removes one saturation point, or one food point if
saturation is empty, once per 20Hz tick. No movement means no distance cost.
Sprinting requires more than 6 food and positive health.

At zero food, every 80 ticks (four game seconds) removes one health point
(half a heart). World's Survival Difficulty defaults to Normal: Easy stops at
10 health, Normal at 1, Hard at 0. Natural Regeneration defaults on: food >=18
and damaged living health restores one health point every 80 ticks, costing
3 exhaustion. Feeding above zero resets starvation timing on the next tick.
There is no passive idle food drain.

Player's authoritative fixed-step adapter runs on the server (or offline).
The local controller accumulates actual collision-resolved ground distance;
remote players use the displacement observed through the existing client-owned
NetworkTransform, plus owner-authorized mode/jump reports. Clients do not send
new food/health or exhaustion amounts. Teleports/large corrections are excluded
from movement cost, and remote jump reports have the existing jump cooldown.
Remote controllers are not also simulated locally by the server. Host players
pay once. Synchronization/sampling can differ from Java; this is not an
anti-cheat movement implementation. The existing flight toggle remains.

SyncVars saturation/exhaustion join health/hunger; the HUD needs no changes.
SetHunger clamps saturation to the new food value. Inspect the spawned player on
a host to set hunger/saturation for testing. Set hunger 0 to test starvation,
or saturation 0/exhaustion 3.99 to see sprint drain quickly. A full new player
must spend the initial five saturation points before visible food falls.
SetHunger(20) from server gameplay code can replenish food, but item eating,
saturation food recipes, XP rewards, underwater exhaustion, difficulty menus,
full death/respawn UI and survival-state save persistence are not implemented.
At health zero the controller stops movement/new world inputs. Existing saves
still only serialize the earlier player data and reset new survival values.

Run `python Tools/HungerChecks/run.py --mcs <compiler> --mono <runtime>`.
Model checks cover thresholds, depletion order, exhaustion cap, movement/jump
costs, flight, sprint cutoff, regeneration, starvation timer and all three damage
floors. Adapter checks compile production Player_Survival and FixedUpdate against
Unity/Mirror doubles, covering host/client/offline authority, real movement vs
walls, teleport exclusion, duplicate jump reports, loading pauses and 50Hz to
20Hz timing. Unity compilation/Mirror weaving and a host+remote-client play test
remain required. Compare movement rates over a measured path before calling the
whole survival controller parity-complete.
