import argparse, subprocess, tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs'); p.add_argument('--mono', default='mono')
a = p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    binary = Path(tmp) / 'creative.exe'
    subprocess.run([a.mcs, '-out:' + str(binary),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerInventory_Creative.cs"),
        str(root / "Assets/_Scripts/Entity's/Player/PlayerHudAssets.cs"),
        str(root / 'Assets/_Scripts/Block/BlockType.cs'),
        str(root / 'Tools/CreativeChecks/CreativeCheck.cs')], check=True)
    subprocess.run([a.mono, str(binary)], check=True)
