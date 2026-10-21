import argparse
import subprocess
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
p = argparse.ArgumentParser()
p.add_argument('--mcs', default='mcs')
p.add_argument('--mono', default='mono')
a = p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    source = Path(tmp) / 'WorldListMenuManager.cs'
    production = root / 'Assets/_Scripts/Menus/MenuManagers/WorldListMenuManager.cs'
    source.write_text('using Environment = MenuEnvironment;\n' + production.read_text(encoding='utf-8-sig'))
    binary = Path(tmp) / 'check.exe'
    subprocess.run([a.mcs, '-out:' + str(binary), str(source), str(root / 'Tools/WorldMenuChecks/Check.cs')], check=True)
    subprocess.run([a.mono, str(binary), tmp], check=True)

# The runtime fallback must point to the existing prefab, preserving its identity.
prefab = root / 'Assets/Prefabs/WorldButtonPrefab.prefab'
guid = next(line.split(': ')[1] for line in Path(str(prefab) + '.meta').read_text().splitlines() if line.startswith('guid:'))
assets = (root / 'Assets/Resources/PlayerHudAssets.asset').read_text()
assert 'worldListEntryPrefab: {fileID: 7926451109967716828, guid: ' + guid + ', type: 3}' in assets
assert '--- !u!1 &7926451109967716828' in prefab.read_text()
print('World-entry prefab resource link verified.')
