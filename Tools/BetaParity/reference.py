#!/usr/bin/env python3
"""Generate a pinned Java reference outside the Unity project; do not use C# as oracle."""
import argparse
from pathlib import Path
import re
import shutil
import subprocess

REV = '4c665027560d63e2afa86a5424fc7d9dce974be6'
URL = 'https://github.com/emortaldev/Minestom173.git'
PKG = 'ca/spottedleaf/oldgenerator/generator/b173'

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--reference', type=Path, required=True, help='Checkout of Minestom173 at pinned revision')
    ap.add_argument('--work', type=Path, required=True, help='Temporary adapter/build/output directory')
    args = ap.parse_args()
    actual = subprocess.check_output(['git', '-C', str(args.reference), 'rev-parse', 'HEAD'], text=True).strip()
    if actual != REV:
        raise SystemExit(f'Reference must be {REV}, got {actual}')
    src = args.reference / 'src/main/java'
    build = args.work / 'src'
    build.mkdir(parents=True, exist_ok=True)
    def write(name, text):
        p = build / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text)
    for p in (src / PKG / 'noise').glob('*.java'):
        write(f'{PKG}/noise/{p.name}', p.read_text())
    for name in ['WorldChunkManager173.java', 'MathHelper173.java', 'MapGenBase173.java']:
        write(f'{PKG}/{name}', (src / PKG / name).read_text())
    write(f'{PKG}/overworld/MapGenCaves173.java', (src / PKG / 'overworld/MapGenCaves173.java').read_text())
    # Keep the Java constructor, density, interpolation, and surface bodies verbatim.
    # Remove server-specific imports, chunk loading/population, and interface only.
    s = (src / PKG / 'overworld/ChunkProviderOverworld173.java').read_text()
    s = s[:s.index('    public void generateUnpopulatedChunkData(')] + '\n}\n'
    s = re.sub(r'^import (?!java\.util\.Random|ca\.spottedleaf\.oldgenerator\.generator\.b173\.(?:BiomeBase173|LegacyUtil173|MapGenBase173|WorldChunkManager173|noise\.NoiseGeneratorOctaves173)|net\.minestom\.server\.instance\.block\.Block).*$\n?', '', s, flags=re.M)
    s = s.replace(' implements IChunkLoader, OldChunkGenerator', '')
    write(f'{PKG}/overworld/ChunkProviderOverworld173.java', s)
    # The reference biome class depends on modern server registries and tree classes.
    # Retain the actual lookup initialization/classification methods without those.
    biome = (src / PKG / 'BiomeBase173.java').read_text()
    lookup = biome[biome.index('    static final BiomeBase173[] LOOKUP'):biome.index('    public WorldGenerator173 getTreeGenerator(Random random, int minHeight, int maxHeight) {', biome.index('    static final BiomeBase173[] LOOKUP'))]
    write(f'{PKG}/BiomeBase173.java', '''package ca.spottedleaf.oldgenerator.generator.b173;
import net.minestom.server.instance.block.Block;
public enum BiomeBase173 {
RAINFOREST, SWAMPLAND, SEASONAL_FOREST, FOREST, SAVANNA, SHRUBLAND, TAIGA, DESERT, PLAINS, ICE_DESERT, TUNDRA, HELL, SKY;
public final Block top = (name().equals("DESERT") || name().equals("ICE_DESERT")) ? Block.SAND : Block.GRASS_BLOCK;
public final Block bottom = (name().equals("DESERT") || name().equals("ICE_DESERT")) ? Block.SAND : Block.DIRT;
''' + lookup + '}\n')
    write('net/minestom/server/instance/block/Block.java', '''package net.minestom.server.instance.block;
public enum Block {
AIR(0), STONE(1), GRASS_BLOCK(2), DIRT(3), BEDROCK(7), WATER(9), LAVA(11), SAND(12), GRAVEL(13), SANDSTONE(24), ICE(79);
public final int id;
Block(int id) { this.id=id; }
public boolean isAir(){ return this==AIR; }
public boolean compare(Block other){ return this==other; }
public interface Getter { Block getBlock(int x,int y,int z); }
public interface Setter { void setBlock(int x,int y,int z,Block block); }
}\n''')
    write(f'{PKG}/LegacyUtil173.java', '''package ca.spottedleaf.oldgenerator.generator.b173;
import net.minestom.server.instance.block.Block;
public class LegacyUtil173 {
public static Block getBlockIndex(Block.Getter g,int i){return g.getBlock(i>>11,i&127,(i>>7)&15);}
public static void setBlockIndex(Block.Setter s,int i,Block b){s.setBlock(i>>11,i&127,(i>>7)&15,b);}
}\n''')
    write('ca/spottedleaf/oldgenerator/util/BlockConstants.java', 'package ca.spottedleaf.oldgenerator.util; public class BlockConstants {}\n')
    write('ReferenceMain.java', (Path(__file__).parent / 'ReferenceMain.java').read_text())
    classes = args.work / 'classes'
    classes.mkdir(exist_ok=True)
    subprocess.run(['java', 'com.sun.tools.javac.Main', '-d', str(classes), *map(str, build.rglob('*.java'))], check=True)
    out = args.work / 'fixtures'
    out.mkdir(exist_ok=True)
    subprocess.run(['java', '-cp', str(classes), 'ReferenceMain', str(out)], check=True)
    print(f'Reference binaries and manifest: {out}')

if __name__ == '__main__':
    main()
