"""Build pinned Beta Java feature bodies against explicitly mocked world services.
No server ticks, fluid physics, or actual chunk lighting are asserted by this oracle.
"""
import argparse, json, subprocess
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('--reference',type=Path,required=True);ap.add_argument('--work',type=Path,required=True);ap.add_argument('--java',default='java');args=ap.parse_args()
rev=subprocess.check_output(['git','-C',str(args.reference),'rev-parse','HEAD'],text=True).strip()
assert rev=='740c583901e1ff1150e9ef37e37dab5bc0e4f807',rev
src=args.reference/'1.7.3-LTS/src/minecraft/net/minecraft/src';out=args.work/'net/minecraft/src';out.mkdir(parents=True,exist_ok=True)
def write(name,text): (out/(name+'.java')).write_text('package net.minecraft.src;\n'+text)
names=['WorldGenMinable','WorldGenClay','WorldGenFlowers','WorldGenTallGrass','WorldGenDeadBush','WorldGenReed','WorldGenCactus','WorldGenPumpkin','WorldGenDungeons','WorldGenLakes','WorldGenLiquids','WorldGenerator','WorldGenTrees','WorldGenBigTree','WorldGenForest','WorldGenTaiga1','WorldGenTaiga2','MathHelper']
for name in names:(out/(name+'.java')).write_text((src/(name+'.java')).read_text())
write('Material','''class Material {final int id;Material(int i){id=i;}boolean isSolid(){return id==1||id==4;}boolean getIsSolid(){return isSolid();}boolean getIsLiquid(){return id==2||id==3;}static Material air=new Material(0),rock=new Material(1),water=new Material(2),lava=new Material(3),ice=new Material(4);}''')
# The shim defines only the world contracts used by the unmodified generators.
write('Block',r'''import java.util.*;
class Block {
 static Block[] blocksList=new Block[256];static boolean[] opaqueCubeLookup=new boolean[256];
 int blockID;Material blockMaterial;Block(int id){blockID=id;blockMaterial=mat(id);blocksList[id]=this;opaqueCubeLookup[id]=opaque(id);}
 static boolean solid(int id){return id!=0&&id!=8&&id!=9&&id!=10&&id!=11&&id!=6&&id!=31&&id!=32&&id!=37&&id!=38&&id!=39&&id!=40&&id!=50&&id!=51&&id!=59&&id!=78&&id!=83;}
 static boolean opaque(int id){return solid(id)&&id!=18&&id!=20&&id!=79&&id!=44&&id!=53&&id!=67&&id!=81&&id!=52;}
 static Material mat(int id){return id==79?Material.ice:id==8||id==9?Material.water:id==10||id==11?Material.lava:solid(id)?Material.rock:Material.air;}
 boolean canBlockStay(World w,int x,int y,int z){int b=w.getBlockId(x,y-1,z);if(blockID==83)return b==83||((b==2||b==3)&&w.waterNear(x,y-1,z));if(blockID==81)return (b==12||b==81)&&!solid(w.getBlockId(x-1,y,z))&&!solid(w.getBlockId(x+1,y,z))&&!solid(w.getBlockId(x,y,z-1))&&!solid(w.getBlockId(x,y,z+1));return true;}
 boolean canPlaceBlockAt(World w,int x,int y,int z){return true;}
 void updateTick(World w,int x,int y,int z,Random r){} // Physics intentionally outside fixture scope.
 static Block stone,dirt,grass,sand,gravel,waterStill,lavaStill,waterMoving,lavaMoving,leaves,wood,blockClay,plantYellow,plantRed,mushroomBrown,mushroomRed,tallGrass,deadBush,reed,cactus,pumpkin,cobblestoneMossy,cobblestone,chest,mobSpawner,oreCoal,oreIron,oreGold,oreRedstone,oreDiamond,oreLapis,snow;
 static {for(int i=0;i<256;i++)new Block(i);stone=blocksList[1];grass=blocksList[2];dirt=blocksList[3];cobblestone=blocksList[4];sand=blocksList[12];gravel=blocksList[13];wood=blocksList[17];leaves=blocksList[18];waterMoving=blocksList[8];waterStill=blocksList[9];lavaMoving=blocksList[10];lavaStill=blocksList[11];blockClay=blocksList[82];plantYellow=new BlockFlower(37);plantRed=new BlockFlower(38);mushroomBrown=new BlockFlower(39);mushroomRed=new BlockFlower(40);tallGrass=new BlockFlower(31);deadBush=new BlockFlower(32);reed=blocksList[83];cactus=blocksList[81];pumpkin=blocksList[86];cobblestoneMossy=blocksList[48];chest=blocksList[54];mobSpawner=blocksList[52];oreCoal=blocksList[16];oreIron=blocksList[15];oreGold=blocksList[14];oreRedstone=blocksList[73];oreDiamond=blocksList[56];oreLapis=blocksList[21];snow=blocksList[78];}
}
class BlockFlower extends Block {BlockFlower(int i){super(i);}boolean canBlockStay(World w,int x,int y,int z){if(y<0||y>=128)return false;int b=w.getBlockId(x,y-1,z);if(blockID==39||blockID==40)return w.getFullBlockLightValue(x,y,z)<13&&opaque(b);boolean soil=blockID==32?b==12:b==2||b==3||b==60;return soil&&(w.getFullBlockLightValue(x,y,z)>=8||w.canBlockSeeTheSky(x,y,z));}}
class Item {int shiftedIndex;Item(int i){shiftedIndex=i;}static Item[] itemsList=new Item[2300];static Item saddle=new Item(329),ingotIron=new Item(265),bread=new Item(297),wheat=new Item(296),gunpowder=new Item(289),silk=new Item(287),bucketEmpty=new Item(325),appleGold=new Item(322),redstone=new Item(331),record13=new Item(2256),dyePowder=new Item(351);static{itemsList[2256]=record13;itemsList[2257]=new Item(2257);}}
class ItemStack {int itemID,stackSize,damage;ItemStack(Item i){this(i,1);}ItemStack(Item i,int n){this(i,n,0);}ItemStack(Item i,int n,int d){itemID=i.shiftedIndex;stackSize=n;damage=d;}}
class TileEntityChest {World w;String key;TileEntityChest(World w,String p){this.w=w;key=p;}int getSizeInventory(){return 27;}void setInventorySlotContents(int slot,ItemStack i){w.records.put(key+",loot,"+slot,i.itemID+","+i.stackSize+","+i.damage);}}
class TileEntityMobSpawner {World w;String key;TileEntityMobSpawner(World w,String p){this.w=w;key=p;}void setMobID(String mob){w.records.put(key+",spawner,0",mob);}}
enum EnumSkyBlock {Sky}
class BlockSand {static boolean fallInstantly;}
class BiomeGenBase {
 static BiomeGenBase rainforest=new BiomeGenBase(0),swampland=new BiomeGenBase(1),seasonalForest=new BiomeGenBase(2),forest=new BiomeGenBase(3),savanna=new BiomeGenBase(4),shrubland=new BiomeGenBase(5),taiga=new BiomeGenBase(6),desert=new BiomeGenBase(7),plains=new BiomeGenBase(8),iceDesert=new BiomeGenBase(9),tundra=new BiomeGenBase(10);
 int kind;BiomeGenBase(int i){kind=i;}WorldGenerator getRandomWorldGenForTrees(Random r){if(kind==3){if(r.nextInt(5)==0)return new WorldGenForest();return r.nextInt(3)==0?new WorldGenBigTree():new WorldGenTrees();}if(kind==6)return r.nextInt(3)==0?new WorldGenTaiga1():new WorldGenTaiga2();if(kind==0)return r.nextInt(3)==0?new WorldGenBigTree():new WorldGenTrees();return r.nextInt(10)==0?new WorldGenBigTree():new WorldGenTrees();}
}
''')
write('World',r'''import java.util.*;
class World {
 final int env;final long seed;BiomeGenBase biome=BiomeGenBase.plains;
 TreeMap<String,Integer> writes=new TreeMap<>();TreeMap<String,String> records=new TreeMap<>();boolean scheduledUpdatesAreImmediate;
 World(long s,int e){seed=s;env=e;}
 String key(int x,int y,int z){return x+","+y+","+z;}
 int base(int x,int y,int z){if(y<0||y>=128)return 0;if(env==4){if(x==10&&y==62&&z==10)return 9;return y<70?12:0;}if(env==5){if(y==63)return Math.floorMod(z,8)<4?9:2;return y<63?1:0;}if(env==2&&y==39&&z==0&&x==4)return 54;if(env==2&&y==39&&z==0&&x==5)return 52;if(env==2&&y>=40&&y<=41&&z==0&&x>=3&&x<=8)return 0;if(env==1){if(y==62&&Math.floorMod(z,8)<4)return 9;if(y==61)return 12;if(y==62)return 2;return y<61?1:0;}if(y==63)return env==3?12:2;return y<63?1:0;}
 int state(int x,int y,int z){return writes.getOrDefault(key(x,y,z),base(x,y,z)*16);}
 int getBlockId(int x,int y,int z){return state(x,y,z)/16;}
 int getBlockMetadata(int x,int y,int z){return state(x,y,z)%16;}
 boolean setBlock(int x,int y,int z,int id){return setBlockAndMetadata(x,y,z,id,0);}
 boolean setBlockAndMetadata(int x,int y,int z,int id,int meta){if(y<0||y>=128)return false;if(id==8)id=9;if(id==10)id=11;String k=key(x,y,z);int s=id*16+meta;if(s==base(x,y,z)*16)writes.remove(k);else writes.put(k,s);return true;}
 boolean setBlockWithNotify(int x,int y,int z,int id){return setBlock(x,y,z,id);}
 boolean isAirBlock(int x,int y,int z){return getBlockId(x,y,z)==0;}
 Material getBlockMaterial(int x,int y,int z){return Block.mat(getBlockId(x,y,z));}
 Object getBlockTileEntity(int x,int y,int z){return getBlockId(x,y,z)==54?new TileEntityChest(this,key(x,y,z)):new TileEntityMobSpawner(this,key(x,y,z));}
 int getHeightValue(int x,int z){for(int y=127;y>=0;y--){int b=getBlockId(x,y,z);if(b!=0&&b!=6&&b!=31&&b!=32&&b!=37&&b!=38&&b!=39&&b!=40&&b!=50&&b!=83&&b!=10&&b!=11&&b!=52&&b!=81&&b!=78)return y+1;}return 0;}
 boolean canBlockSeeTheSky(int x,int y,int z){return y>=getHeightValue(x,z);}
 int getFullBlockLightValue(int x,int y,int z){return canBlockSeeTheSky(x,y,z)?15:0;}
 int getSavedLightValue(EnumSkyBlock sky,int x,int y,int z){return getFullBlockLightValue(x,y,z);}
 boolean waterNear(int x,int y,int z){return getBlockMaterial(x-1,y,z)==Material.water||getBlockMaterial(x+1,y,z)==Material.water||getBlockMaterial(x,y,z-1)==Material.water||getBlockMaterial(x,y,z+1)==Material.water;}
 long getRandomSeed(){return seed;}
 World getWorldChunkManager(){return this;}
 BiomeGenBase getBiomeGenAt(int x,int z){return biome;}
 double[] getTemperatures(double[] a,int x,int z,int w,int h){a=new double[w*h];Arrays.fill(a,env==3?.2:.8);return a;}
 int findTopSolidBlock(int x,int z){for(int y=127;y>0;y--)if(Block.solid(getBlockId(x,y,z))||getBlockMaterial(x,y,z).getIsLiquid())return y+1;return -1;}
}
''')
pop=(src/'ChunkProviderGenerate.java').read_text();pop=pop[pop.index('    public void populate('):pop.index('    public boolean saveChunks(')]
pop=pop.replace('IChunkProvider var1','Object var1')
write('Pipeline','import java.util.*;class Pipeline {World worldObj;Random rand;double[] generatedTemperatures;Noise mobSpawnerNoise=new Noise();Pipeline(World w,long s){worldObj=w;rand=new Random(s);}\n'+pop+'}\nclass Noise {double func_806_a(double x,double z){return 0;}}')
write('Oracle',r'''import java.util.*;import java.security.*;import java.nio.charset.StandardCharsets;
public class Oracle {
 static String hash(World w)throws Exception{StringBuilder b=new StringBuilder();for(var e:w.writes.entrySet())b.append(e.getKey()).append(',').append(e.getValue()).append('\n');for(var e:w.records.entrySet())b.append(e.getKey()).append(',').append(e.getValue()).append('\n');byte[] d=MessageDigest.getInstance("SHA-256").digest(b.toString().getBytes(StandardCharsets.UTF_8));StringBuilder s=new StringBuilder();for(byte v:d)s.append(String.format("%02x",v));return s.toString();}
 public static void main(String[] args)throws Exception {
  for(int env=0;env<6;env++)for(long seed:new long[]{0,1,2,17,42,123456789,-1,Long.MIN_VALUE})
  for(String feature:new String[]{"ore","clay","lake","lava","dungeon","flower","mushroom","grass","fern","bush","reeds","cactus","pumpkin","spring"}){
   World w=new World(seed,env);Random r=new Random(seed);int x=0,y=64,z=0;if(feature.equals("ore"))y=20;if(feature.equals("clay")){x=10;y=62;z=10;}if(feature.equals("dungeon")||feature.equals("mushroom"))y=40;if(feature.equals("spring")){x=2;y=40;}
   WorldGenerator gen=switch(feature){case "ore"->new WorldGenMinable(56,7);case "clay"->new WorldGenClay(32);case "lake"->new WorldGenLakes(9);case "lava"->new WorldGenLakes(11);case "dungeon"->new WorldGenDungeons();case "flower"->new WorldGenFlowers(37);case "mushroom"->new WorldGenFlowers(39);case "grass"->new WorldGenTallGrass(31,1);case "fern"->new WorldGenTallGrass(31,2);case "bush"->new WorldGenDeadBush(32);case "reeds"->new WorldGenReed();case "cactus"->new WorldGenCactus();case "pumpkin"->new WorldGenPumpkin();default->new WorldGenLiquids(8);};
   boolean result=gen.generate(w,r,x,y,z);long next=r.nextLong();System.out.println(feature+"\t"+seed+"\t"+env+"\t"+x+"\t"+y+"\t"+z+"\t"+result+"\t"+next+"\t"+w.writes.size()+"\t"+hash(w));
  }
  for(int env=0;env<6;env++)for(long seed:new long[]{0,17,123456789,-1})for(int b:new int[]{0,1,2,3,4,5,6,7,8,9,10}){
   World w=new World(seed,env);w.biome=new BiomeGenBase[]{BiomeGenBase.rainforest,BiomeGenBase.swampland,BiomeGenBase.seasonalForest,BiomeGenBase.forest,BiomeGenBase.savanna,BiomeGenBase.shrubland,BiomeGenBase.taiga,BiomeGenBase.desert,BiomeGenBase.plains,BiomeGenBase.iceDesert,BiomeGenBase.tundra}[b];Pipeline p=new Pipeline(w,seed);p.populate(null,-1,2);long next=p.rand.nextLong();System.out.println("pipeline"+b+"\t"+seed+"\t"+env+"\t-1\t0\t2\ttrue\t"+next+"\t"+w.writes.size()+"\t"+hash(w));
  }
 }
}
''')
# Freeze source IDs/metadata mapping for the C# side as well as fixture schema.
files=list(out.glob('*.java'));subprocess.run([args.java,'-m','jdk.compiler/com.sun.tools.javac.Main','-d',str(args.work/'classes'),*map(str,files)],check=True)
with (args.work/'fixtures.tsv').open('w') as f:subprocess.run([args.java,'-cp',str(args.work/'classes'),'net.minecraft.src.Oracle'],stdout=f,check=True)
print(args.work/'fixtures.tsv')
