import argparse,gzip,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[2]
ap=argparse.ArgumentParser();ap.add_argument('--mcs',default='mcs');ap.add_argument('--mono',default='mono');a=ap.parse_args()
beta=root/'Assets/_Scripts/WorldGeneration/Beta'
raw=gzip.decompress((root/'Tools/PopulationParity/fixtures.tsv.gz').read_bytes())
rows=[line.split('\t') for line in raw.decode().splitlines()]
assert len(rows)==936, 'fixture coverage changed'
for feature in ['ore','clay','lake','lava','dungeon','mushroom','reeds','cactus','pumpkin','spring']:
 assert any(row[0]==feature and int(row[8])>0 for row in rows), feature+' lacks a successful placement fixture'
with tempfile.TemporaryDirectory() as tmp:
 tmp=Path(tmp)
 source=(beta/'Beta173Population.cs').read_text()
 methods=source[source.index('    private JavaRandom PopulationRandom'):source.index('    // Tree-only preview')]
 methods+=source[source.index('    private static long MakeOdd'):source.index('    private static bool IsAir')]
 (tmp/'Population.cs').write_text('public sealed partial class Beta173Population{private readonly long worldSeed;public Beta173Population(long seed){worldSeed=seed;}'+methods+'}')
 source=(beta/'BetaPopulationPipeline.cs').read_text()
 (tmp/'Pipeline.cs').write_text('using System;public sealed partial class Beta173Population{'+source[source.index('    public void PopulateComplete(IBetaPopulationWorld'):])
 sources=[beta/(n+'.cs') for n in ['BetaPopulationFeatures','Beta173Trees','JavaRandom','BetaMathHelper','BetaBiome']]
 sources += [root/'Assets/_Scripts/Block/BlockType.cs',root/'Assets/_Scripts/Block/BetaBlockCatalog.cs',root/'Tools/PopulationParity/PopulationCheck.cs',tmp/'Population.cs',tmp/'Pipeline.cs']
 exe=tmp/'population.exe';subprocess.run([a.mcs,'-langversion:latest','-checked+','-out:'+str(exe),*map(str,sources)],check=True)
 fixture=tmp/'fixtures.tsv';fixture.write_bytes(raw)
 subprocess.run([a.mono,str(exe),str(fixture)],check=True)
