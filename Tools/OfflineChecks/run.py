import argparse
import subprocess
import tempfile
from pathlib import Path

root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('--mcs',default='mcs')
parser.add_argument('--mono',default='mono')
args=parser.parse_args()
with tempfile.TemporaryDirectory() as temporary:
    executable=Path(temporary)/'offline.exe'
    sources=['Assets/_Scripts/PlayerIdentity.cs','Assets/_Scripts/Networking/MinecraftNetworkManager.cs','Tools/OfflineChecks/OfflineCheck.cs']
    subprocess.run([args.mcs,'-langversion:latest','-out:'+str(executable),*[str(root/source) for source in sources]],check=True)
    subprocess.run([args.mono,str(executable)],check=True)

# Startup, spawn and player setup must all use the same captured identity.
for name in ["Assets/_Scripts/Entity's/Player/Player.cs",'Assets/_Scripts/World/World_Generation.cs']:
    source=(root/name).read_text()
    assert 'SteamClient.SteamId' not in source and 'SteamClient.Name' not in source,name
    assert 'PlayerIdentity.Id' in source,name
for name in ['WorldCreationMenuManager','WorldListMenuManager']:
    source=(root/('Assets/_Scripts/Menus/MenuManagers/'+name+'.cs')).read_text()
    assert 'NetworkManager.singleton.StartHost();' not in source,name
    assert 'MinecraftNetworkManager.StartGameHost();' in source,name
print('World creation/loading, spawn and player identity wiring verified.')
