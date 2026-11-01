# Steam friends multiplayer

The Multiplayer list discovers lobbies through each Steam friend's active game,
requests their metadata, and refreshes every five seconds while the menu is open.
It lists ready, non-full games owned by a direct Steam friend, deduplicating a
lobby shared by several friends. Friends playing solo or sitting in menus do not
appear. Games from other projects using test App ID 480 are filtered out by the
project/protocol tags. Both players need this updated build; older public lobbies
without the tags will not appear.

Hosting through Multiplayer creates a friends-only lobby (up to eight players,
bounded by Mirror's configured capacity). It becomes joinable when the host world
is created. Solo worlds keep Mirror's in-process host and never create a lobby.
The existing FizzyFacepunch Steam relay transport handles the remote connection.
Guests connect to the original owner's Steam ID; lobby ownership migration does
not move the world server to another player. Closing the host ends the session.

Accepting a Steam lobby invitation in the main menu uses the same join validation.
Steam callback work is queued onto Unity's main thread. Duplicate joins, late
callbacks after cancellation, changed owners, incompatible settings, and failed
connections leave the lobby cleanly. Joining remote terrain uses a distinct local
world namespace so cached local entities are not restored into a friend's world.
The project/protocol tags filter discovery; they are not transport authentication.
Bump `LobbyManager.Protocol` when changing incompatible network messages.

## Automated checks

Run:

```sh
python Tools/SteamMultiplayerChecks/run.py --mcs <compiler> --mono <runtime>
python Tools/OfflineChecks/run.py --mcs <compiler> --mono <runtime>
```

The first compiles production lobby/browser/invitation/button code against the
actual bundled Facepunch Win64 SDK, with Unity/Mirror doubles. It then exercises
production sessions and browser behavior against native-free Steam doubles:
host publication/readiness/cleanup, host entry ordering, delayed metadata,
duplicate clicks, cancel and late completion, full/closed/incompatible lobbies,
owner changes, connection timeout, seed handoff, friend filtering/deduplication,
selection refresh, stale results, and untrusted display text. The offline suite
runs the production network manager and confirms solo hosting and identity still
work without Steam. The actual Unity scene lifecycle and native transport are
not executed by these checks.

## Two-PC test

1. Build the same revision for both PCs. Run Steam on both with **different
   accounts that are Steam friends**, and launch the game with the bundled native
   Steam library. The configured development App ID is 480; use the same App ID
   on both machines. Steam presence must expose each account's current game.
2. Host: open Multiplayer, create/name a lobby, and create or select a world.
   Wait until terrain and the host player have loaded.
3. Friend: open Multiplayer. Within a refresh cycle, the host's lobby, world name,
   Steam name and member count should appear. Select it and press Join, or
   double-click the row. No IP address or manual port forwarding is required by
   the configured Steam relay path.
4. Check that both players see each other, block edits are shared, and each
   player keeps their own inventory. Disconnect and rejoin; verify player saves
   and edits persist. Repeat on different internet connections to exercise relay.
5. Check cancel during joining, host closing while a guest connects, host exit,
   and hosting again without restarting the app. Guests should return to menus;
   stale host rows should disappear on refresh. No new world is hosted by a
   guest after a disconnect.
6. As a control, start a solo world with Steam running: friends should not see it.
   Start solo with Steam closed as well. Other App 480 games must never appear.

A real two-account/two-PC connection has not been executed in this workspace.
For a failed attempt, collect both Unity Player.log files, noting whether the
lobby was visible, whether Steam entry succeeded, and the first transport or
world-startup error. On Windows the logs are under
`%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Player.log`.
