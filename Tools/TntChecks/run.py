import argparse,subprocess,tempfile,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
with tempfile.TemporaryDirectory() as tmp:
    exe=Path(tmp)/'tnt.exe'
    farm=(root/'Assets/_Scripts/World/World_Farming.cs').read_text()
    ignition=farm[farm.index('    public bool UseFarmItem('):farm.index('        if(held.type==ItemType.WaterBucket')]
    adapter=Path(tmp)/'Ignition.cs';adapter.write_text('using UnityEngine;public partial class World{'+ignition+'return false;} }')
    files=['Tools/Shared/UnityVectors.cs','Tools/VehicleChecks/Check.cs','Tools/TntChecks/Check.cs','Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/ItemType.cs','Assets/_Scripts/World/VehiclePhysics.cs','Assets/_Scripts/World/World_Vehicles.cs','Assets/_Scripts/Block/BlockCollision.cs',"Assets/_Scripts/Entity's/Player/BlockTargeting.cs","Assets/_Scripts/Entity's/Player/Player_Vehicles.cs",'Assets/_Scripts/World/Rendering/VehicleVisual.cs','Assets/_Scripts/World/Rendering/VehicleModelGeometry.cs','Assets/_Scripts/World/World_Tnt.cs','Assets/_Scripts/World/Rendering/PrimedTntVisual.cs','Assets/_Scripts/WorldGeneration/Beta/JavaRandom.cs']
    subprocess.run([a.mcs,'-langversion:latest','-main:TntCheck','-out:'+str(exe),*[str(root/f) for f in files],str(adapter)],check=True)
    subprocess.run([a.mono,str(exe)],check=True)
for source in ['World_HostileMobs.cs','World_NetherBed.cs']:
    text=(root/'Assets/_Scripts/World'/source).read_text();assert 'EmitExplosionEffects(center,' in text and 'PrimeTnt(p,true)' in text
assert 'if(block.type==BlockType.TNT)' in (root/'Assets/_Scripts/World/World_Farming.cs').read_text()
assert 'primedTnt = CaptureTnt()' in (root/'Assets/_Scripts/World/World_Serialization.cs').read_text()
assert '_FuseFlash' in (root/'Assets/Shaders/DroppedBlock.shader').read_text()
for sound in ['random/fuse.ogg','fire/ignite.ogg','random/explode1.ogg']:assert (root/'Assets/Resources/Sounds'/sound).exists()
print('Existing blast particle/chain hooks, TNT ignition/save/flash and sound assets verified.')
for script in ['World/World_Tnt.cs','World/Rendering/PrimedTntVisual.cs']:
    meta=(root/'Assets/_Scripts'/(script+'.meta')).read_text()
    assert re.search(r'^guid: [0-9a-f]{32}$',meta,re.M),script+' has an invalid Unity import GUID'
print('Both TNT script import GUIDs are valid 32-character identifiers.')
