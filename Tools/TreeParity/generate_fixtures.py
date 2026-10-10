#!/usr/bin/env python3
from pathlib import Path
import argparse, subprocess, tempfile, gzip
ap=argparse.ArgumentParser(description='Build pinned Java tree fixtures with Bukkit storage doubles')
ap.add_argument('--reference',type=Path,required=True,help='OldGenerator checkout at the pinned commit')
ap.add_argument('--output',type=Path,default=Path(__file__).with_name('fixtures.tsv.gz'))
args=ap.parse_args()
root=args.reference.resolve()
PIN='8c4ad258a380e9461d00d72ec7c9af9006bc050a'
if subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD'],text=True).strip()!=PIN:
 raise SystemExit('Reference checkout must be pinned to '+PIN)
tmp=tempfile.TemporaryDirectory(prefix='tree-java-reference-')
out=Path(tmp.name)
def write(name,content):
 p=out/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(content)
base='src/main/java/ca/spottedleaf/oldgenerator/generator/b173/'
for name in ['populator/WorldGenTrees173.java','populator/WorldGenBigTree173.java','populator/WorldGenForest173.java','populator/WorldGenTaiga1173.java','populator/WorldGenTaiga2173.java','populator/WorldGenerator173.java','BiomeBase173.java','MathHelper173.java']:
 p=root/base/name;write('ca/spottedleaf/oldgenerator/generator/b173/'+name,p.read_text())
write('org/bukkit/Material.java','''package org.bukkit; public enum Material { NOTHING(0), AIR(1), DIRT(2), GRASS_BLOCK(3), STONE(4), OAK_LOG(5), OAK_LEAVES(6), SAND(7), WATER(8), GLASS(9), ICE(16), SPRUCE_LOG(17), SPRUCE_LEAVES(18), BIRCH_LOG(19), BIRCH_LEAVES(20); public final int id; Material(int id){this.id=id;} }''')
write('org/bukkit/block/data/BlockData.java','package org.bukkit.block.data; import org.bukkit.Material; public class BlockData { public final Material material; public BlockData(Material material){this.material=material;} }')
write('org/bukkit/block/Biome.java','package org.bukkit.block; public enum Biome { JUNGLE,SWAMP,FOREST,SAVANNA,PLAINS,SNOWY_TAIGA,DESERT,SNOWY_PLAINS,NETHER_WASTES,THE_VOID }')
write('ca/spottedleaf/oldgenerator/world/BlockAccess.java','''package ca.spottedleaf.oldgenerator.world; import org.bukkit.Material; import org.bukkit.block.data.BlockData; public interface BlockAccess { int getMinHeight(); int getMaxHeight(); Material getType(int x,int y,int z); void setBlockData(int x,int y,int z,BlockData data,boolean physics); }''')
write('ca/spottedleaf/oldgenerator/util/LeafDistanceCalculator.java','''package ca.spottedleaf.oldgenerator.util; import ca.spottedleaf.oldgenerator.world.BlockAccess; public class LeafDistanceCalculator { public void addLog(int x,int y,int z){} public void update(BlockAccess world){} }''')
materials=['AIR','DIRT','GRASS_BLOCK','SAND','OAK_LOG','OAK_LEAVES','BIRCH_LOG','BIRCH_LEAVES','SPRUCE_LOG','SPRUCE_LEAVES']
write('ca/spottedleaf/oldgenerator/util/BlockConstants.java','''package ca.spottedleaf.oldgenerator.util; import org.bukkit.Material; import org.bukkit.block.data.BlockData; public class BlockConstants { '''+''.join(f'public static final BlockData {m}=new BlockData(Material.{m});' for m in materials)+''' public static boolean isAir(Material m){return m==Material.AIR;} public static boolean isLeaves(Material m){return m==Material.OAK_LEAVES || m==Material.SPRUCE_LEAVES || m==Material.BIRCH_LEAVES;} }''')
write('ca/spottedleaf/oldgenerator/generator/b173/LegacyUtil173.java','''package ca.spottedleaf.oldgenerator.generator.b173; import org.bukkit.Material; import ca.spottedleaf.oldgenerator.util.BlockConstants; public class LegacyUtil173 { public static boolean Block_o(Material m){return m!=Material.AIR && m!=Material.WATER && m!=Material.GLASS && !BlockConstants.isLeaves(m);} }''')
provider=(root/base/'overworld/ChunkProviderGenerate173.java').read_text();a=provider.index('        d0 = 0.5D;',provider.index('WorldGenMinable173(BlockConstants.LAPIS'));b=provider.index('        int k2;',a)
count=provider[a:b].replace('        d0 = 0.5D;','').replace('k1 = (int)','int k1 = (int)').replace('this.treeCountNoise.generateNoiseForCoordinate((double) k * d0, (double) l * d0)','noise').replace('this.random','random').replace('l1 = 0;','int l1 = 0;')
write('CountReference.java','import java.util.Random; import ca.spottedleaf.oldgenerator.generator.b173.BiomeBase173; public class CountReference { static int count(BiomeBase173 biomebase,double noise,Random random){'+count+'return l1;} }')
write('Oracle.java',r'''import java.util.*;
import java.security.*;
import java.nio.charset.StandardCharsets;
import ca.spottedleaf.oldgenerator.generator.b173.populator.*;
import ca.spottedleaf.oldgenerator.generator.b173.BiomeBase173;
import ca.spottedleaf.oldgenerator.world.BlockAccess;
import org.bukkit.Material;
import org.bukkit.block.data.BlockData;
public class Oracle {
 static class Pos implements Comparable<Pos> { int x,y,z; Pos(int x,int y,int z){this.x=x;this.y=y;this.z=z;} public int compareTo(Pos p){int c=Integer.compare(x,p.x);if(c==0)c=Integer.compare(y,p.y);if(c==0)c=Integer.compare(z,p.z);return c;} }
 static class World implements BlockAccess {
  int env,x,y,z; TreeMap<Pos,Material> writes=new TreeMap<>();
  World(int env,int x,int y,int z){this.env=env;this.x=x;this.y=y;this.z=z;}
  public int getMinHeight(){return 0;} public int getMaxHeight(){return 127;}
  public Material getType(int a,int b,int c){Material m=writes.get(new Pos(a,b,c));if(m!=null)return m;
   if(b<0||b>=128)return Material.AIR;
   if(env==1 && a==x && c==z && b==y+4)return Material.STONE;
   if(env==2 && a==x && c==z && b==y+8)return Material.STONE;
   if(env==3 && a==x+2 && c==z+2 && b==y+4)return Material.STONE;
   if(env==5 && b==y-1)return Material.SAND;
   if(env==6 && a==x && c==z && b==y)return Material.WATER;
   if(env==7 && b>=y && b<y+2 && Math.abs(a-x)<=2 && Math.abs(c-z)<=2)return Material.OAK_LEAVES;
   if(env==8 && a==x && c==z && b==y+1)return Material.OAK_LOG;
   if(env==9 && a==x+2 && c==z && b==y+5)return Material.GLASS;
   if(env==10 && a==x-2 && c==z-2 && b==y+4)return Material.WATER;
   if(b==y-1)return Material.GRASS_BLOCK;
   return b<y-1?Material.STONE:Material.AIR;
  }
  public void setBlockData(int a,int b,int c,BlockData data,boolean physics){if(b>=0&&b<128)writes.put(new Pos(a,b,c),data.material);}
  String hash()throws Exception {StringBuilder s=new StringBuilder();for(var e:writes.entrySet()){Pos p=e.getKey();s.append(p.x+","+p.y+","+p.z+","+e.getValue().id+"\n");}byte[] b=MessageDigest.getInstance("SHA-256").digest(s.toString().getBytes(StandardCharsets.UTF_8));StringBuilder h=new StringBuilder();for(byte v:b)h.append(String.format("%02x",v&255));return h.toString();}
 }
 static WorldGenerator173 generator(int kind){switch(kind){case 1:return new WorldGenBigTree173();case 2:return new WorldGenForest173();case 3:return new WorldGenTaiga1173();case 4:return new WorldGenTaiga2173();default:return new WorldGenTrees173();}}
 static int kind(WorldGenerator173 g){if(g instanceof WorldGenBigTree173)return 1;if(g instanceof WorldGenForest173)return 2;if(g instanceof WorldGenTaiga1173)return 3;if(g instanceof WorldGenTaiga2173)return 4;return 0;}
 public static void main(String[] args)throws Exception {
  System.out.println("# OldGenerator 8c4ad258a380e9461d00d72ec7c9af9006bc050a tree fixtures; Bukkit storage mocked");
  for(int k=0;k<5;k++)for(int env=0;env<12;env++)for(int i=0;i<128;i++){
   long seed=i==127?Long.MIN_VALUE:((long)i*132897987541L-341873128712L);
   int x=env==11?-17:0,z=env==11?-33:0,y=env==4?122:64;
   World w=new World(env,x,y,z);Random r=new Random(seed);WorldGenerator173 g=generator(k);g.scale(1,1,1);
   boolean success=g.populate(w,r,x,y,z);
   System.out.println("tree\t"+k+"\t"+env+"\t"+seed+"\t"+x+"\t"+y+"\t"+z+"\t"+success+"\t"+w.writes.size()+"\t"+w.hash()+"\t"+r.nextLong());
  }
  BiomeBase173[] biomes={BiomeBase173.RAINFOREST,BiomeBase173.SWAMPLAND,BiomeBase173.SEASONAL_FOREST,BiomeBase173.FOREST,BiomeBase173.SAVANNA,BiomeBase173.SHRUBLAND,BiomeBase173.TAIGA,BiomeBase173.DESERT,BiomeBase173.PLAINS,BiomeBase173.ICE_DESERT,BiomeBase173.TUNDRA};
  for(int b=0;b<biomes.length;b++)for(int seed=0;seed<128;seed++){Random r=new Random(seed);System.out.println("select\t"+b+"\t"+seed+"\t"+kind(biomes[b].getTreeGenerator(r))+"\t"+r.nextLong());}
  for(int b=0;b<biomes.length;b++)for(int seed=0;seed<16;seed++)for(double noise:new double[]{-200,-64,-24,0,64,120}){Random r=new Random(seed);System.out.println("count\t"+b+"\t"+seed+"\t"+noise+"\t"+CountReference.count(biomes[b],noise,r)+"\t"+r.nextLong());}
 }
}''')
files=[str(p) for p in out.rglob('*.java')]
subprocess.run(['java','com.sun.tools.javac.Main',*files],check=True)
fixture=out/'fixtures.tsv'
with fixture.open('w') as f:
 subprocess.run(['java','-cp',str(out),'Oracle'],stdout=f,check=True)
args.output.write_bytes(gzip.compress(fixture.read_bytes(),mtime=0))
print('Fixtures written to',args.output)
tmp.cleanup()
