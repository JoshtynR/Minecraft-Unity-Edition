#!/usr/bin/env python3
"""Extract unchanged Beta brightness/AO methods into a tiny Java reference runner.
No reconstructed Java source is copied into the Unity project.
"""
import argparse,re,subprocess,tempfile,gzip
from pathlib import Path
PIN='740c583901e1ff1150e9ef37e37dab5bc0e4f807'
def main():
 ap=argparse.ArgumentParser();ap.add_argument('reference',type=Path);ap.add_argument('--output',type=Path,default=Path(__file__).with_name('fixtures.tsv.gz'));a=ap.parse_args()
 assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=a.reference,text=True).strip()==PIN
 base=a.reference/'1.7.3-LTS/src/minecraft/net/minecraft/src'
 src=(base/'RenderBlocks.java').read_text();method=src[src.index('    public boolean renderStandardBlockWithAmbientOcclusion'):src.index('    public boolean renderStandardBlockWithColorMultiplier')]
 provider=(base/'WorldProvider.java').read_text();brightness=provider[provider.index('    protected void generateLightBrightnessTable()'):provider.index('    protected void registerWorldChunkManager()')]
 fields=set(re.findall(r'this\.(\w+)',method));methods={x for x in fields if x.startswith('render') and x.endswith('Face')};excluded=methods|{'blockAccess','overrideBlockTexture','field_22352_G','renderAllFaces','enableAO'}
 bools={x for x in fields if re.search(r'this\.'+x+r' = Block.canBlockGrass',method)}
 decl='\n'.join(('boolean ' if x in bools else 'float ')+x+';' for x in sorted(fields-excluded))
 render_parts=[]
 for m in sorted(methods):
  start=src.index('    public void '+m+'(');brace=src.index('{',start);depth=1;end=brace+1
  while depth:
   if src[end]=='{':depth+=1
   elif src[end]=='}':depth-=1
   end+=1
  face={'renderBottomFace':0,'renderTopFace':1,'renderEastFace':2,'renderWestFace':3,'renderNorthFace':4,'renderSouthFace':5}[m]
  part=src[start:end];part=part[:part.index('{')+1]+f' Tessellator.face={face}; output({face}); '+part[part.index('{')+1:]
  render_parts.append(part)
 render='\n'.join(render_parts)
 extra_fields=set(re.findall(r'this\.(\w+)',render))-fields
 extra='\n'.join(('boolean ' if x=='flipTexture' else 'int ')+x+';' for x in sorted(extra_fields))
 code='''import java.util.*;
class Tessellator {static Tessellator instance=new Tessellator();static int face;float red;void setColorOpaque_F(float r,float g,float b){red=r;}void addVertexWithUV(double x,double y,double z,double u,double v){System.out.println("O\\t"+Ref.caseId+"\\t"+face+"\\t"+(int)Math.round(x)+"\\t"+(int)Math.round(y)+"\\t"+(int)Math.round(z)+"\\t"+red);}}
class Access { int[] sky=new int[27],block=new int[27];boolean[] pass=new boolean[27];int dark;int index(int x,int y,int z){return (x+1)*9+(y+1)*3+z+1;} int getBlockId(int x,int y,int z){return pass[index(x,y,z)]?0:1;} }
class Block {static boolean[] canBlockGrass={true,false};int blockIndexInTexture=0,emission;double minX,minY,minZ,maxX=1,maxY=1,maxZ=1;boolean shouldSideBeRendered(Access a,int x,int y,int z,int f){return true;}int getBlockTexture(Access a,int x,int y,int z,int f){return 0;}
float getBlockBrightness(Access a,int x,int y,int z){int i=a.index(x,y,z);int v=Math.max(Math.max(0,a.sky[i]-a.dark),Math.max(a.block[i],emission));return Ref.table[v];}}
public class Ref {
static float[] table=new float[16];float[] lightBrightnessTable=table;Access blockAccess;int overrideBlockTexture=-1,field_22352_G=1;boolean renderAllFaces=true,enableAO;static boolean fancyGrass=false;
'''+decl+'\n'+extra+'\n'+brightness+'\n'+method+'\n'+render+'''
static int caseId;void output(int face){float[] v={colorRedTopLeft,colorRedBottomLeft,colorRedBottomRight,colorRedTopRight};Arrays.sort(v);System.out.print("A\\t"+caseId+"\\t"+face);for(float f:v)System.out.print("\\t"+f);System.out.println();}
public static void main(String[] args){Ref r=new Ref();r.generateLightBrightnessTable();for(int i=0;i<16;i++)System.out.println("B\\t"+i+"\\t"+table[i]);Random random=new Random(173);for(caseId=0;caseId<256;caseId++){Access a=new Access();a.dark=random.nextInt(12);Block b=new Block();b.emission=caseId%4==0?15:0;System.out.print("W\\t"+caseId+"\\t"+a.dark+"\\t"+b.emission);for(int i=0;i<27;i++){a.sky[i]=random.nextInt(16);a.block[i]=random.nextInt(16);a.pass[i]=random.nextBoolean();System.out.print("\\t"+a.sky[i]+","+a.block[i]+","+(a.pass[i]?1:0));}System.out.println();r.blockAccess=a;r.renderStandardBlockWithAmbientOcclusion(b,0,0,0,1,1,1);}}
}
'''
 with tempfile.TemporaryDirectory() as tmp:
  tmp=Path(tmp);(tmp/'Ref.java').write_text(code)
  subprocess.run(['java','com.sun.tools.javac.Main',str(tmp/'Ref.java')],check=True)
  output=subprocess.check_output(['java','-cp',str(tmp),'Ref'],text=True)
 with a.output.open('wb') as f:
  with gzip.GzipFile(filename='',mode='wb',fileobj=f,mtime=0) as g:g.write(('# reference '+PIN+'\n'+output).encode())
 print('Wrote 16 brightness entries, 1536 six-face AO cases and 6144 positioned vertex colors.')
if __name__=='__main__':main()
