import java.io.*;
import java.util.*;

// Independent evaluation of Chunk.generateSkylightMap/relightBlock's vertical
// attenuation rules in the pinned Beta 1.7.3 reference. A short synthetic
// column has a bedrock bottom, as naturally generated Beta columns do.
class BetaColumnReference {
    public static void main(String[] args) throws Exception {
        Random random=new Random(173);
        try(PrintWriter output=new PrintWriter(args[0])) {
            for(int test=0;test<64;test++) {
                int[] opacity=new int[16];
                for(int y=1;y<16;y++)opacity[y]=new int[]{0,0,0,0,1,3,15}[random.nextInt(7)];
                if(test<4)Arrays.fill(opacity,0);
                opacity[0]=15;
                if(test==1)opacity[14]=1;
                if(test==2)opacity[14]=opacity[10]=opacity[6]=3;
                if(test==3)opacity[8]=15;
                for(boolean nether:new boolean[]{false,true})for(boolean changed:new boolean[]{false,true}) {
                    int level=nether?0:15;int[] values=new int[16];
                    for(int y=15;y>=0;y--) {
                        int cost=opacity[y];if(changed&&level<15)cost=Math.max(1,cost);
                        level=Math.max(0,level-cost);values[y]=level;
                    }
                    output.print((nether?1:0)+"\t"+(changed?1:0)+"\t");
                    for(int y=0;y<16;y++)output.print((y==0?"":",")+opacity[y]);output.print("\t");
                    for(int y=0;y<16;y++)output.print((y==0?"":",")+values[y]);output.println();
                }
            }
        }
    }
}
