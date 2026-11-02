"""Compile the actual transport against the bundled SDK; exercise without native Steam."""
import argparse
from pathlib import Path
import subprocess
import tempfile
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs')
p.add_argument('--mono', default='mono')
a = p.parse_args()
folder = root / 'Assets/Mirror/Runtime/Transport/FizzyFacepunch'
sources = [*[f for f in folder.glob('*.cs') if not f.name.startswith('Legacy')], root / 'Assets/Mirror/Runtime/Transport/Transport.cs',
           root / 'Tools/SteamTransportChecks/Doubles.cs']
with tempfile.TemporaryDirectory() as tmp:
    subprocess.run([a.mcs, '-langversion:latest', '-target:library', '-define:SDK',
                    '-r:' + str(root / 'Assets/Plugins/FacePunchSteamWorks/Facepunch.Steamworks.Win64.dll'),
                    '-out:' + tmp + '/sdk.dll', *map(str, sources)], check=True)
    binary = tmp + '/checks.exe'
    subprocess.run([a.mcs, '-langversion:latest', '-out:' + binary, *map(str, sources),
                    str(root / 'Tools/SteamTransportChecks/Check.cs')], check=True)
    subprocess.run([a.mono, binary], check=True)
print('SteamSockets transport compiles against the shipped Facepunch SDK and production Mirror Transport base.')
