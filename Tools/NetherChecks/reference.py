from pathlib import Path
import subprocess,hashlib,argparse
parser=argparse.ArgumentParser()
parser.add_argument('--reference',type=Path,required=True,help='Pinned Minestom173 checkout')
parser.add_argument('--support',type=Path,required=True,help='Work directory produced by Tools/BetaParity/reference.py')
parser.add_argument('--work',type=Path,required=True)
args=parser.parse_args()
repo=Path(__file__).resolve().parents[2]
pin='4c665027560d63e2afa86a5424fc7d9dce974be6'
assert subprocess.check_output(['git','-C',str(args.reference),'rev-parse','HEAD'],text=True).strip()==pin,'Unexpected reference revision'
out=args.work;out.mkdir(parents=True,exist_ok=True)
ref=args.reference/'src/main/java/ca/spottedleaf/oldgenerator/generator/b173'
s=(ref/'nether/ChunkProviderHell173.java').read_text();s=s[:s.index('    public void generateUnpopulatedChunkData')]+ '}\n'
s='\n'.join(line for line in s.splitlines() if not line.startswith('import ') or any(t in line for t in ['LegacyUtil173','MapGenBase173','noise.NoiseGeneratorOctaves173','instance.block.Block','java.util.Random']))
s=s.replace(' implements IChunkLoader, OldChunkGenerator','')
s='\n'.join(line for line in s.splitlines() if 'worldChunkManager' not in line)
(out/'ChunkProviderHell173.java').write_text(s)
(out/'MapGenCavesHell173.java').write_text((ref/'nether/MapGenCavesHell173.java').read_text())
# Extend a tiny reference Block enum; all generator implementation methods stay unchanged.
s=(args.support/'src/net/minestom/server/instance/block/Block.java').read_text().replace('ICE(79);','ICE(79), NETHERRACK(87), SOUL_SAND(88);')
(out/'Block.java').write_text(s)
(out/'NetherReference.java').write_text('''import ca.spottedleaf.oldgenerator.generator.b173.nether.*;
import net.minestom.server.instance.block.Block;import java.util.*;import java.lang.reflect.*;import java.security.*;
public class NetherReference {
 static void emit(long seed,int x,int z,String stage,Block[] blocks)throws Exception {byte[] b=new byte[32768];for(int i=0;i<b.length;i++)b[i]=(byte)blocks[i].id;byte[] h=MessageDigest.getInstance("SHA-256").digest(b);StringBuilder hex=new StringBuilder();for(byte v:h)hex.append(String.format("%02x",v));System.out.println(seed+"\\t"+x+"\\t"+z+"\\t"+stage+"\\t"+hex);}
 public static void main(String[] args)throws Exception {long[] seeds={0,1,-1,12345,Long.MIN_VALUE,Long.MAX_VALUE};int[][] chunks={{0,0},{1,-1},{-1,1},{17,29},{-7,-3},{784426,0},{-784426,0}};
 for(long seed:seeds)for(int[] c:chunks){int x=c[0],z=c[1];var p=new ChunkProviderHell173(0,127,seed);Block[] b=new Block[32768];Arrays.fill(b,Block.AIR);Block.Getter get=(xx,y,zz)->b[(xx*16+zz)*128+y];Block.Setter set=(xx,y,zz,t)->b[(xx*16+zz)*128+y]=t;
 p.generateBareTerrain(x,z,set);emit(seed,x,z,"raw",b);Field f=p.getClass().getDeclaredField("random");f.setAccessible(true);((Random)f.get(p)).setSeed((long)x*341873128712L+(long)z*132897987541L);p.generateBiomeTerrain(x,z,get,set);emit(seed,x,z,"surface",b);new MapGenCavesHell173().generate(get,set,0,127,seed,x,z);emit(seed,x,z,"caves",b);}}
}''')
classes=out/'classes';classes.mkdir(exist_ok=True)
subprocess.run(['java','com.sun.tools.javac.Main','-cp',str(args.support/'classes'),'-d',str(classes),*[str(out/f) for f in ['Block.java','ChunkProviderHell173.java','MapGenCavesHell173.java','NetherReference.java']]],check=True)
with (repo/'Tools/NetherChecks/terrain-fixtures.tsv').open('w') as f:subprocess.run(['java','-cp',str(classes)+':'+str(args.support/'classes'),'NetherReference'],stdout=f,check=True)
