import argparse,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--mcs',default='mcs');p.add_argument('--mono',default='mono');p.add_argument('--locate',type=int,help='Find a village for this numeric world seed');p.add_argument('--regions',type=int,default=2);a=p.parse_args()
names=['JavaRandom','BetaNoise','BetaMathHelper','BetaClimate','BetaBiome','Beta173Terrain','Beta173Caves','BetaSurfaceDecorator','MinecraftSeed','BetaCoordinateSpace','Beta173Trees','Beta173NetherTerrain','Beta173NetherCaves','Beta173NetherPopulation','Beta173Population','BetaPopulationFeatures','BetaPopulationPipeline','BetaPopulationAdapter','BetaPopulationRecord','BetaParityDump','Beta18Villages','BetaVillageTerrain']
sources=[root/('Assets/_Scripts/WorldGeneration/Beta/'+name+'.cs') for name in names]
sources += [root/path for path in ['Tools/VillageChecks/Check.cs','Tools/BetaParity/IntegrationCheck.cs','Assets/Editor/BetaParityCheck.cs','Assets/_Scripts/WorldGeneration/TerrainGenerator.cs','Assets/_Scripts/Block/BlockType.cs','Assets/_Scripts/Block/BetaBlockCatalog.cs','Assets/_Scripts/World/ChunkData_Villages.cs','Assets/_Scripts/World/Saving/ChunkSaveData.cs']]
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'villages.exe'
    subprocess.run([a.mcs,'-langversion:latest','-checked+','-main:VillageCheck','-out:'+str(exe),*map(str,sources)],check=True)
    command=[a.mono,str(exe)]
    if a.locate is not None:command += ['--locate',str(a.locate),str(max(0,min(a.regions,8)))]
    subprocess.run(command,check=True)
# New generated masks persist, while legacy chunks are never retrofitted.
assert 'chunkData.RestoreVillageProtection(saveData.betaVillageProtection)' in (root/'Assets/_Scripts/World/ChunkData.cs').read_text()
print('Village protection reload hook verified.')
