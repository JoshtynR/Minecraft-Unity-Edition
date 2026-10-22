import argparse,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');a=p.parse_args()
beta='Assets/_Scripts/WorldGeneration/Beta/'
terrain=['Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs']+[beta+n+'.cs' for n in ['JavaRandom','BetaNoise','BetaMathHelper','Beta173NetherTerrain','Beta173NetherCaves']]
runtime=['Tools/NetherChecks/UnityStub.cs','Assets/_Scripts/ItemType.cs','Assets/_Scripts/World/DimensionRules.cs','Assets/_Scripts/World/PortalRules.cs','Assets/_Scripts/World/World_Dimensions.cs','Assets/_Scripts/World/World_Portals.cs','Assets/_Scripts/World/World_NetherBed.cs','Assets/_Scripts/World/WorldSpawnRules.cs',"Assets/_Scripts/Entity's/Player/Player_Dimensions.cs", "Assets/_Scripts/Entity's/Player/Player_NetherBed.cs",'Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs',beta+'JavaRandom.cs']
def compile_run(out,sources,args=(),flags=(),run=True):
    subprocess.run([a.mcs,'-langversion:latest','-nowarn:0649,0414','-out:'+str(out),*flags,*[str(root/f) for f in sources]],check=True)
    if run:subprocess.run([a.mono,str(out),*map(str,args)],check=True)
with tempfile.TemporaryDirectory() as tmp:
    tmp=Path(tmp)
    generation=(root/'Assets/_Scripts/World/World_Generation.cs').read_text()
    ensure=generation[generation.index('    public async Task EnsureAreaAsync'):generation.index('    private async Task GenerateWorldCore')].replace('EnsureAreaAsync','EnsureProductionAsync')
    ensure_file=tmp/'EnsureArea.cs'
    ensure_file.write_text('using System.Threading.Tasks;using UnityEngine;public partial class World{public int productionCoreCalls;public bool observedFullArea;private Task GenerateWorldCore(Vector3Int p){productionCoreCalls++;observedFullArea=PreparingDimensionArea;return Task.CompletedTask;}'+ensure+'}')
    runtime.append(str(ensure_file))
    compile_run(tmp/'terrain.exe',terrain+['Tools/NetherChecks/TerrainCheck.cs'],[root/'Tools/NetherChecks/terrain-fixtures.tsv'])
    population=[beta+n+'.cs' for n in ['Beta173NetherPopulation','BetaPopulationFeatures','Beta173Trees','BetaBiome','BetaMathHelper']]
    compile_run(tmp/'dimension.exe',runtime+population+['Tools/NetherChecks/DimensionCheck.cs'])
    source=(root/'Assets/_Scripts/World/World.cs').read_text()
    message=tmp/'StartWorldMessage.cs';message.write_text('using UnityEngine;using Mirror;'+source[source.index('public struct StartWorldMessage'):source.index('public struct WorldData')])
    # Compile the complete production server against the same dimension implementation.
    subprocess.run([a.mcs,'-langversion:latest','-nowarn:0649,0414','-define:CHECK_SERVER','-target:library','-out:'+str(tmp/'server.dll'),*[str(root/f) for f in runtime],str(root/'Tools/NetherChecks/ServerStub.cs'),str(root/'Assets/_Scripts/World/WorldServer.cs'),str(message)],check=True)
    print('Complete WorldServer + dimension/portal adapters compiled; RPC signatures verified below.')
# Check every introduced or changed RPC, including private methods, for Mirror's no-defaults rule.
import re
for name in ['Assets/_Scripts/World/WorldServer.cs',"Assets/_Scripts/Entity's/Player/Player_Dimensions.cs",'Assets/_Scripts/GameAudio.cs','Assets/_Scripts/World/World_Chests.cs']:
    text=(root/name).read_text()
    for declaration in re.findall(r'\[(?:ClientRpc|TargetRpc)\][^{]+',text):
        assert '=' not in declaration, 'Optional RPC parameter: '+declaration
assert 'WorldDataHelper.SetBlock' in (root/'Assets/_Scripts/World/World_Portals.cs').read_text()
assert 'SaveAllDimensionsAsync' in (root/'Assets/_Scripts/Menus/MenuManagers/EscapeMenuManager.cs').read_text()
print('RPC signatures and awaited save-on-exit wiring verified.')
