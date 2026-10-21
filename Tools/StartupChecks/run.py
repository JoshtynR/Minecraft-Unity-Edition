#!/usr/bin/env python3
import argparse
import subprocess
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs')
p.add_argument('--mono', default='mono')
args = p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    binary = Path(tmp) / 'startup.exe'
    files = ['Assets/_Scripts/MusicManager.cs', 'Assets/_Scripts/WorldLoadingScreen.cs', 'Assets/_Scripts/Networking/MinecraftNetworkManager.cs',
             'Assets/_Scripts/Block/BlockType.cs',
             'Tools/StartupChecks/UnityStub.cs', 'Tools/StartupChecks/StartupCheck.cs']
    subprocess.run([args.mcs, '-langversion:latest', '-out:' + str(binary), *[str(root / f) for f in files]], check=True)
    subprocess.run([args.mono, str(binary)], check=True)

# Verify the production entry points remain connected to the persistent screen.
player = (root / "Assets/_Scripts/Entity's/Player/Player.cs").read_text()
assert 'public bool IsReadyForPlay => clientInitialized && !dimensionTransfer && !loadingSavedPlayer && (!expectsSavedPlayerData || savedPlayerApplied);' in player
assert player.index('clientInitialized = true;') > player.index('_normalFOV = _camComponent.fieldOfView;')
load = player[player.index('    public void LoadPlayer('):player.index('    private float lastForwardTapTime')]
assert load.index('loadingSavedPlayer = true;') < load.index('ExecuteAfterFrames')
assert load.index('loadingSavedPlayer = false;') > load.index('inventory.LoadInventory(')
assert load.index('savedPlayerApplied = true;') > load.index('inventory.LoadInventory(')
server = (root / 'Assets/_Scripts/World/WorldServer.cs').read_text()
spawn = server[server.index('    public void SpawnPlayerMessageHandler('):server.index('    public struct SpawnPlayerMessage')]
assert spawn.index('expectsSavedPlayerData = true;') < spawn.index('NetworkServer.AddPlayerForConnection(')
assert spawn.index('PrepareDimensionJoin();') < spawn.index('NetworkServer.AddPlayerForConnection(')
assert player.count('if (WorldLoadingScreen.IsLoading && (!networkStarted || isLocalPlayer)) return;') == 2
assert 'if (WorldLoadingScreen.IsLoading) return;' in (root / "Assets/_Scripts/Entity's/Player/PlayerInventory.cs").read_text()
network = (root / 'Assets/_Scripts/Networking/MinecraftNetworkManager.cs').read_text()
assert network.count('WorldLoadingScreen.Begin();') == 3
assert 'WorldLoadingScreen.Cancel();' in network
assert 'WorldLoadingScreen.Begin();' in (root / 'Assets/_Scripts/World/World.cs').read_text()
assert 'WorldLoadingScreen.Begin();' in (root / 'Assets/_Scripts/Menus/Menu.cs').read_text()
scene=(root/'Assets/Scenes/World.unity').read_text()
preview=scene.split('--- !u!20 &1755582243\n')[1].split('--- !u!')[0]
ui_camera=scene.split('--- !u!20 &1556104983\n')[1].split('--- !u!')[0]
assert '  m_Enabled: 0' in preview and '  m_Enabled: 1' in ui_camera
background_guid=next(line.split(': ')[1] for line in (root/'Assets/Textures/sprites/options_background.png.meta').read_text().splitlines() if line.startswith('guid:'))
assert '  loadingBackground: {fileID: 2800000, guid: '+background_guid in (root/'Assets/Resources/PlayerHudAssets.asset').read_text()
print('Player initialization, saved-player restore, gameplay guards, and scene/network entry points verified.')

subprocess.run(['python3',str(root/'Tools/StartupChecks/save_check.py'),'--mcs',args.mcs,'--mono',args.mono],check=True)
