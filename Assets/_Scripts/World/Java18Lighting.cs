using System;

// Java 1.8's separate sky/block lightmap, in display (gamma) color space.
// Independent of Unity so the renderer and reference checks share the same math.
public static class Java18Lighting
{
    public struct RGB
    {
        public float r,g,b;
        public RGB(float red,float green,float blue){r=red;g=green;b=blue;}
    }
    private static float Clamp(float v)=>Math.Max(0,Math.Min(1,v));
    public static float Level(int level,bool nether)
    {
        float inverse=1-Math.Max(0,Math.Min(15,level))/15f;
        float ambient=nether?.1f:0;
        return (1-inverse)/(inverse*3+1)*(1-ambient)+ambient;
    }
    public static float SunBrightness(float celestialDegrees)
    {return Clamp((float)Math.Cos(celestialDegrees*Math.PI/180)*2+.2f)*.8f+.2f;}
    private static float Finish(float value,float gamma)
    {
        value=Clamp(value);float inverse=1-value;
        value=value+(1-inverse*inverse*inverse*inverse-value)*Clamp(gamma);
        // Vanilla stores truncated eight-bit channels in its dynamic texture.
        return (int)(Clamp(value*.96f+.03f)*255)/255f;
    }
    public static RGB Sample(int sky,int block,float sun=1,float gamma=0,bool nether=false,float flicker=0)
    {
        sun=Clamp(sun);
        float skyValue=Level(sky,nether)*(sun*.95f+.05f);
        float torch=Level(block,nether)*(flicker*.1f+1.5f);
        float skyRG=skyValue*(sun*.65f+.35f);
        float green=torch*((torch*.6f+.4f)*.6f+.4f);
        float blue=torch*(torch*torch*.6f+.4f);
        return new RGB(Finish((skyRG+torch)*.96f+.03f,gamma),
            Finish((skyRG+green)*.96f+.03f,gamma),
            Finish((skyValue+blue)*.96f+.03f,gamma));
    }
}
