import java.io.*;

// Standalone evaluation of 1.8 WorldProvider's brightness table and
// EntityRenderer.updateLightmap (without potion, weather or boss modifiers).
class Reference {
    public static void main(String[] args) throws Exception {
        try(PrintWriter out=new PrintWriter(args[0])) {
            for(boolean nether:new boolean[]{false,true})
            for(float sun:new float[]{1f,.2f,.5f,.73f})
            for(float gamma:new float[]{0,.5f,1})
            for(float flicker:new float[]{-.7f,0,.5f}) {
                float[] table=new float[16];float ambient=nether?.1f:0;
                for(int level=0;level<16;level++) {
                    float inverse=1f-(float)level/15f;
                    table[level]=(1f-inverse)/(inverse*3f+1f)*(1f-ambient)+ambient;
                }
                for(int sky=0;sky<16;sky++)for(int block=0;block<16;block++) {
                    float s=table[sky]*(sun*.95f+.05f);
                    float t=table[block]*(flicker*.1f+1.5f);
                    float red=s*(sun*.65f+.35f)+t;
                    float green=s*(sun*.65f+.35f)+t*((t*.6f+.4f)*.6f+.4f);
                    float blue=s+t*(t*t*.6f+.4f);
                    int packed=0;
                    for(float c:new float[]{red,green,blue}) {
                        c=c*.96f+.03f;c=Math.min(1,c);
                        float inverse=1-c;
                        c=c*(1-gamma)+(1-inverse*inverse*inverse*inverse)*gamma;
                        c=Math.max(0,Math.min(1,c*.96f+.03f));
                        packed=(packed<<8)|(int)(c*255);
                    }
                    out.println((nether?1:0)+"\t"+sun+"\t"+gamma+"\t"+flicker+"\t"+sky+"\t"+block+"\t"+packed);
                }
            }
        }
    }
}
