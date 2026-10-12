import argparse,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
base=ROOT/"Assets/_Scripts/Entity's/Player"
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'mining.exe'
 sources=[base/n for n in ['HandMining.cs','Player_Mining.cs','BlockBreakOverlay.cs','PlayerHudAssets.cs','HungerSimulation.cs','SurvivalHudValues.cs']]+[ROOT/p for p in ['Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BlockShape.cs','Assets/_Scripts/World/Rendering/MeshData.cs','Tools/MiningChecks/MiningCheck.cs']]
 subprocess.run([a.mcs,'-langversion:latest','-out:'+str(binary),*[str(s) for s in sources]],check=True)
 subprocess.run([a.mono,str(binary)],check=True)
