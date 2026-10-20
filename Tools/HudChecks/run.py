import argparse, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');args=ap.parse_args()
with tempfile.TemporaryDirectory() as tmp:
 binary=Path(tmp)/'hud.exe'
 base=ROOT/"Assets/_Scripts/Entity's/Player"
 subprocess.run([args.mcs,'-out:'+str(binary),str(base/'SurvivalHudValues.cs'),str(base/'PlayerHudAssets.cs'),str(base/'PlayerSurvivalHUD.cs'),str(ROOT/'Assets/_Scripts/MinecraftUIText.cs'),str(ROOT/'Tools/HudChecks/HudCheck.cs')],check=True)
 subprocess.run([args.mono,str(binary)],check=True)
