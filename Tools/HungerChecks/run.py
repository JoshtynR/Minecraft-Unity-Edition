import argparse, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');args=ap.parse_args()
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'hunger.exe'
 base=ROOT/"Assets/_Scripts/Entity's/Player"
 subprocess.run([args.mcs,'-out:'+str(binary),str(base/'SurvivalHudValues.cs'),str(base/'HungerSimulation.cs'),str(ROOT/'Tools/HungerChecks/HungerCheck.cs')],check=True)
 subprocess.run([args.mono,str(binary)],check=True)

with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'adapter.exe'
 base=ROOT/"Assets/_Scripts/Entity's/Player"
 player=(base/'Player.cs').read_text()
 method=player[player.index('    public override void FixedUpdate()'):player.index('    public bool PlaceBlock()')]
 adapter=Path(tmp)/'PlayerFixed.cs';adapter.write_text('using UnityEngine;public partial class Player{'+method+'}')
 subprocess.run([args.mcs,'-langversion:latest','-out:'+str(binary),str(base/'SurvivalHudValues.cs'),str(base/'HungerSimulation.cs'),str(base/'Player_Survival.cs'),str(adapter),str(ROOT/'Tools/HungerChecks/IntegrationCheck.cs')],check=True)
 subprocess.run([args.mono,str(binary)],check=True)
