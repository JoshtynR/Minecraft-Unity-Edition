"""Run the production spawn selector against controlled terrain columns."""
import argparse
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--mcs', default='mcs')
parser.add_argument('--mono', default='mono')
args = parser.parse_args()
with tempfile.TemporaryDirectory() as directory:
    binary = Path(directory) / 'spawn.exe'
    files = ['Assets/_Scripts/World/WorldSpawnRules.cs', 'Assets/_Scripts/Block/BlockType.cs',
             'Tools/Shared/UnityVectors.cs', 'Tools/SpawnChecks/Check.cs']
    subprocess.run([args.mcs, '-langversion:latest', '-out:' + str(binary),
                    *[str(root / path) for path in files]], check=True)
    subprocess.run([args.mono, str(binary)], check=True)
