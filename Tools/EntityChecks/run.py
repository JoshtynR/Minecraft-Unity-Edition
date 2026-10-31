import argparse,subprocess,tempfile,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    exe=Path(tmp)/'entities.exe'
    files=['Tools/Shared/UnityVectors.cs','Tools/EntityChecks/Check.cs','Assets/_Scripts/World/MobDefinition.cs','Assets/_Scripts/World/VehiclePhysics.cs','Assets/_Scripts/EntityDataSO.cs','Assets/Editor/EntityDataEditor.cs']
    subprocess.run([a.mcs,'-langversion:latest','-out:'+str(exe),*[str(root/f) for f in files]],check=True)
    subprocess.run([a.mono,str(exe)],check=True)
asset=(root/'Assets/Resources/Entity Data.asset').read_text()
assert re.findall(r'^  - species: (\d+)$',asset,re.M)==list(map(str,range(8)))
assert 'guid: af21187bc5704bbe9abe5518e255a58c' in asset
assert 'Time.deltaTime*LidSpeed' in (root/'Assets/_Scripts/World/Rendering/ModernChestVisual.cs').read_text()
assert 'spawnedInChunk<packCap' in (root/'Assets/_Scripts/World/World_MobSpawning.cs').read_text()
print('All eight committed entries, asset binding, pack-cap and chest animation hooks verified.')
