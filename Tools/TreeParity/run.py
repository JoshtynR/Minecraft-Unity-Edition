#!/usr/bin/env python3
"""Compare production Beta tree shapes and RNG use with pinned Java fixtures."""
import argparse
import gzip
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[2]
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--mcs',default='mcs')
    ap.add_argument('--mono',default='mono')
    args=ap.parse_args()
    with tempfile.TemporaryDirectory(prefix='tree-parity-') as tmp:
        exe=Path(tmp)/'trees.exe'
        sources=[ROOT/'Tools/TreeParity/TreeParityCheck.cs',ROOT/'Assets/_Scripts/Block/BlockType.cs']
        sources += [ROOT/f'Assets/_Scripts/WorldGeneration/Beta/{n}.cs' for n in ['Beta173Trees','JavaRandom','BetaMathHelper','BetaBiome']]
        subprocess.run([args.mcs,'-langversion:latest','-checked+',f'-out:{exe}',*map(str,sources)],check=True)
        fixture=Path(tmp)/'fixtures.tsv'
        fixture.write_bytes(gzip.decompress((ROOT/'Tools/TreeParity/fixtures.tsv.gz').read_bytes()))
        subprocess.run([args.mono,str(exe),str(fixture)],check=True)
        integration=Path(tmp)/'tree-integration.exe'
        names=['Beta173Trees','JavaRandom','BetaMathHelper','BetaBiome','Beta173Population','BetaPopulationFeatures','BetaPopulationPipeline','BetaPopulationAdapter','BetaPopulationRecord','Beta173Terrain','BetaClimate','BetaNoise']
        sources=[ROOT/'Assets/_Scripts/Block/BetaBlockCatalog.cs',ROOT/'Tools/TreeParity/TreeIntegrationCheck.cs',ROOT/'Assets/_Scripts/Block/BlockType.cs',ROOT/'Assets/_Scripts/World/Saving/ChunkSaveData.cs']
        sources += [ROOT/f'Assets/_Scripts/WorldGeneration/Beta/{n}.cs' for n in names]
        subprocess.run([args.mcs,'-langversion:latest','-checked+',f'-out:{integration}',*map(str,sources)],check=True)
        subprocess.run([args.mono,str(integration)],check=True)
if __name__=='__main__':main()
