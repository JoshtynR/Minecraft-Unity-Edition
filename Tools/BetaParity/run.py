#!/usr/bin/env python3
"""Compile production generator sources and compare their exact stage hashes."""
import argparse
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
FILES = ['JavaRandom', 'BetaNoise', 'BetaMathHelper', 'BetaClimate', 'BetaBiome', 'Beta173Terrain', 'Beta173Caves', 'BetaSurfaceDecorator', 'MinecraftSeed', 'BetaCoordinateSpace']

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--mcs', default='mcs', help='Mono C# compiler')
    ap.add_argument('--mono', default='mono', help='Mono runtime')
    ap.add_argument('--manifest', type=Path, default=ROOT / 'Tools/BetaParity/fixtures.tsv')
    ap.add_argument('--output', type=Path, help='Optional binary stage outputs for detailed comparison')
    args = ap.parse_args()
    with tempfile.TemporaryDirectory(prefix='beta-parity-') as tmp:
        binary = Path(tmp) / 'parity.exe'
        sources = [ROOT / 'Assets/Editor/BetaParityCheck.cs', ROOT / 'Assets/_Scripts/Block/BlockType.cs']
        sources += [ROOT / f'Assets/_Scripts/WorldGeneration/Beta/{name}.cs' for name in FILES]
        subprocess.run([args.mcs, '-langversion:latest', '-checked+', f'-out:{binary}', *map(str,sources)], check=True)
        cmd = [args.mono, str(binary), str(args.manifest.resolve())]
        if args.output: cmd.append(str(args.output.resolve()))
        subprocess.run(cmd, check=True)
        # Use the production chunk-column copier, with only world/Unity storage mocked.
        biome = (ROOT / 'Assets/_Scripts/WorldGeneration/BiomeGenerator.cs').read_text(encoding='utf-8-sig')
        start = biome.index('    public ChunkData ProcessBetaTerrainColumn(')
        end = biome.index('    public void ProcessFeatures', start)
        adapter = Path(tmp) / 'BiomeIntegration.cs'
        adapter.write_text('using UnityEngine; public class BiomeGenerator {\n' + biome[start:end] + '}\n')
        integration = Path(tmp) / 'integration.exe'
        extra = [ROOT / 'Tools/BetaParity/IntegrationCheck.cs', adapter,
                 ROOT / 'Assets/_Scripts/WorldGeneration/TerrainGenerator.cs',
                 ROOT / 'Assets/_Scripts/WorldGeneration/Beta/Beta173Population.cs',
                 ROOT / 'Assets/_Scripts/WorldGeneration/Beta/BetaParityDump.cs']
        subprocess.run([args.mcs, '-langversion:latest', '-checked+', '-main:IntegrationCheck', f'-out:{integration}', *map(str, sources + extra)], check=True)
        subprocess.run([args.mono, str(integration), str(args.manifest.resolve())], check=True)


if __name__ == '__main__':
    main()
