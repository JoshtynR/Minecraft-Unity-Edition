using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public static class ExplosionParticleCheck
{
    private static object Field(object instance,string name)=>instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(instance);
    public static void Main()
    {
        Resources.items["PlayerHudAssets"]=new PlayerHudAssets{particleAtlas=new Texture2D{width=256,height=256},fluidFizz=new AudioClip(),fluidSplash=new AudioClip()};
        var root=new GameObject("Explosion fixture");var effects=root.AddComponent<FluidEffects>();
        effects.EmitExplosion(Vector3.zero,4,new[]{new Vector3Int(4,2,-3),new Vector3Int(6,3,-4)});
        var layers=(Array)Field(effects,"effects");var smoke=(IList)Field(layers.GetValue(0),"particles");var bursts=(IList)Field(layers.GetValue(5),"particles");
        if(smoke.Count!=2||bursts.Count!=2)throw new Exception("one smoke/explode pair per affected cell");
        for(int i=0;i<2;i++)
        {
            var p=(Vector3)Field(smoke[i],"position");var midpoint=(Vector3)Field(bursts[i],"position");var motion=(Vector3)Field(bursts[i],"velocity");
            int x=i==0?4:6,y=i==0?2:3,z=i==0?-3:-4;
            if(p.x<x||p.x>x+1||p.y<y||p.y>y+1||p.z<z||p.z>z+1)throw new Exception("smoke position must originate within affected cell");
            if((midpoint-p*.5f).magnitude>.00001f)throw new Exception("explode particle must originate at source midpoint");
            // NewExplosion adds +/- .05 jitter after the outward source impulse;
            // a weak component can legitimately reverse sign. Check its range.
            float baseSpeed=.5f/(p.magnitude/4+.1f);
            var direction=p/p.magnitude;
            float[] actual={motion.x,motion.y,motion.z},axis={direction.x,direction.y,direction.z};
            for(int a=0;a<3;a++)
            {
                float low=Math.Min(axis[a]*baseSpeed*.3f,axis[a]*baseSpeed*1.3f)-.05001f;
                float high=Math.Max(axis[a]*baseSpeed*.3f,axis[a]*baseSpeed*1.3f)+.05001f;
                if(actual[a]<low||actual[a]>high)throw new Exception("source blast impulse plus bounded particle jitter");
            }
        }
        Console.WriteLine("7 source-style affected-cell particle placement/velocity checks passed (Unity rendering mocked).");
    }
}
