"""Compile against the shipped Steam SDK, then exercise sessions with native-free doubles."""
import argparse
import subprocess
import tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs')
p.add_argument('--mono', default='mono')
a = p.parse_args()
sources = [root / f for f in [
    'Assets/_Scripts/Menus/LobbyManager.cs',
    'Assets/_Scripts/Menus/LobbyButton.cs',
    'Assets/_Scripts/Menus/MenuManagers/MenuManager.cs',
    'Assets/_Scripts/Menus/MenuManagers/MultiplayerMenuManager.cs',
    'Tools/SteamMultiplayerChecks/Doubles.cs']]
with tempfile.TemporaryDirectory() as tmp:
    subprocess.run([a.mcs, '-langversion:latest', '-target:library', '-define:SDK',
                    '-r:' + str(root / 'Assets/Plugins/FacePunchSteamWorks/Facepunch.Steamworks.Win64.dll'),
                    '-out:' + tmp + '/sdk-check.dll', *map(str, sources)], check=True)
    binary = tmp + '/sessions.exe'
    subprocess.run([a.mcs, '-langversion:latest', '-out:' + binary, *map(str, sources),
                    str(root / 'Tools/SteamMultiplayerChecks/Check.cs')], check=True)
    subprocess.run([a.mono, binary], check=True)
print('Production lobby, browser, invitation and button code compile against the shipped Steam SDK.')

# Loading-menu cancel must address the surviving menu owner, not a destroyed
# scene-local NetworkManager. Preserve the serialized Join/Refresh bindings.
scene = (root / 'Assets/Scenes/MainMenu.unity').read_text()
cancel = scene.split('--- !u!1001 &1987828940')[1].split('--- !u!224 &1987828941')[0]
assert 'value: CancelLobby' in cancel and 'objectReference: {fileID: 1827945858}' in cancel
assert 'value: MenuManager, Assembly-CSharp' in cancel
assert 'm_MethodName: GetLobbys' in scene and 'value: PressSelectedLobby' in scene
print('Existing menu Join/Refresh bindings and loading-menu cancellation verified.')
