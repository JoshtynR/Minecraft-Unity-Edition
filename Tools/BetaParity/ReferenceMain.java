import ca.spottedleaf.oldgenerator.generator.b173.*;
import ca.spottedleaf.oldgenerator.generator.b173.overworld.*;
import ca.spottedleaf.oldgenerator.generator.b173.noise.*;
import net.minestom.server.instance.block.Block;
import java.lang.reflect.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;

public class ReferenceMain {
    static Path output;
    static StringBuilder manifest = new StringBuilder("# seed\tchunkX\tchunkZ\tstage\tsha256 (doubles little-endian IEEE754; blocks Beta IDs x,z,y)\n");
    static String key;
    static long seed;
    static int cx, cz;
    static Object field(Object obj, String name) throws Exception {
        Field f = obj.getClass().getDeclaredField(name); f.setAccessible(true); return f.get(obj);
    }
    static void emit(String stage, double[] values) throws Exception {
        ByteBuffer b = ByteBuffer.allocate(values.length * 8).order(ByteOrder.LITTLE_ENDIAN);
        for(double v:values) b.putDouble(v);
        emit(stage,b.array());
    }
    static void emit(String stage, byte[] bytes) throws Exception {
        byte[] hash = MessageDigest.getInstance("SHA-256").digest(bytes);
        StringBuilder hex = new StringBuilder(); for(byte v:hash) hex.append(String.format("%02x",v));
        manifest.append(seed+"\t"+cx+"\t"+cz+"\t"+stage+"\t"+hex+"\n");
        Files.write(output.resolve(key+"-"+stage+".bin"),bytes);
    }
    public static void main(String[] args) throws Exception {
        output=Paths.get(args[0]);
        long[] seeds={0L,1L,-1L,12345L,8675309L,-1446162294L,Long.MIN_VALUE,Long.MAX_VALUE};
        int[][] chunks={{0,0},{1,-1},{-1,1},{-3,-7},{17,29},{784426,0},{-784426,0}};
        for(long s:seeds) for(int[] chunk:chunks) {
            seed=s; cx=chunk[0]; cz=chunk[1]; key=seed+"_"+cx+"_"+cz;
            ChunkProviderOverworld173 p = new ChunkProviderOverworld173(0,127,seed);
            WorldChunkManager173 climate = (WorldChunkManager173)field(p,"worldChunkManager");
            BiomeBase173[] biomes=climate.getBiomeNoise(null,cx*16,cz*16,16,16);
            emit("temperature",climate.temperature); emit("humidity",climate.rain);
            byte[] biomeBytes=new byte[256]; for(int i=0;i<256;i++) biomeBytes[i]=(byte)biomes[i].ordinal();
            emit("biomes",biomeBytes);
            final Block[] blocks = new Block[32768]; Arrays.fill(blocks,Block.AIR);
            Block.Getter get=(x,y,z)->blocks[(x*16+z)*128+y];
            Block.Setter set=(x,y,z,b)->blocks[(x*16+z)*128+y]=b;
            p.generateBareTerrain(cx,cz,biomes,climate.temperature,set);
            String[][] fields={{"scale","terrainNoise4"},{"depth","terrainNoise5"},{"selector","terrainNoise1"},{"min","terrainNoise2"},{"max","terrainNoise3"},{"density","terrainNoise"}};
            for(String[] f:fields) emit(f[0],(double[])field(p,f[1]));
            double[] treeNoise = new double[25];
            NoiseGeneratorOctaves173 trees=(NoiseGeneratorOctaves173)field(p,"treeCountNoise");
            for(int i=0;i<5;i++) for(int j=0;j<5;j++) treeNoise[i*5+j]=trees.generateNoiseForCoordinate((cx*16+i)*0.5,(cz*16+j)*0.5);
            emit("treeCount",treeNoise);
            ByteBuffer rng=ByteBuffer.allocate(64*28).order(ByteOrder.LITTLE_ENDIAN);
            Random random=new Random(seed);
            int[] bounds={1,2,3,5,16,255,256,1073741825,2147483647};
            for(int i=0;i<64;i++) {
                rng.putInt(random.nextInt()); rng.putInt(random.nextInt(bounds[i%bounds.length]));
                rng.putLong(random.nextLong()); rng.putFloat(random.nextFloat()); rng.putDouble(random.nextDouble());
            }
            emit("random",rng.array());
            String[] seedTexts={"1","+42","-1446162294","9223372036854775807","-9223372036854775808","9223372036854775808"," 42 ","1,000","Glacier","🧱","-0","+","-"};
            ByteBuffer parsed=ByteBuffer.allocate(seedTexts.length*8).order(ByteOrder.LITTLE_ENDIAN);
            for(String text:seedTexts) {
                long value; try {value=Long.parseLong(text);} catch(NumberFormatException ex){value=text.hashCode();}
                parsed.putLong(value);
            }
            emit("seedParsing",parsed.array());
            ByteBuffer mapped=ByteBuffer.allocate(16*16*8).order(ByteOrder.LITTLE_ENDIAN);
            for(int i=0;i<16;i++) for(int j=0;j<16;j++) {mapped.putInt(-(cx*16+i)-1);mapped.putInt(cz*16+j);}
            emit("coordinateMapping",mapped.array());

            emitBlocks("raw",blocks);
            ((Random)field(p,"random")).setSeed((long)cx*341873128712L+(long)cz*132897987541L);
            p.generateBiomeTerrain(cx,cz,biomes,get,set);
            emit("sand",(double[])field(p,"sandNoise")); emit("gravel",(double[])field(p,"gravelNoise")); emit("stoneDepth",(double[])field(p,"stoneNoise"));
            emitBlocks("surface",blocks);
            new MapGenCaves173().generate(get,set,0,127,seed,cx,cz);
            emitBlocks("caves",blocks);
        }
        Files.writeString(output.resolve("fixtures.tsv"),manifest.toString());
        System.out.println("Generated "+seeds.length*chunks.length+" Java reference cases.");
    }
    static void emitBlocks(String name,Block[] blocks) throws Exception {
        byte[] bytes=new byte[blocks.length]; for(int i=0;i<blocks.length;i++) bytes[i]=(byte)blocks[i].id; emit(name,bytes);
    }
}
