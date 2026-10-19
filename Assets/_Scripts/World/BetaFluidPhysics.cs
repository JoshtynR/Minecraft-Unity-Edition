using System;

// Motion is in blocks per Minecraft tick (20 Hz), independent of Unity's
// configurable physics interval. EntityLiving/BlockFluid, Beta 1.7.3.
public struct FluidVector
{
    public double x,y,z;
    public FluidVector(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
    public double Length => Math.Sqrt(x*x+y*y+z*z);
    public FluidVector Normalized(){double n=Length;return n<1e-8?new FluidVector():new FluidVector(x/n,y/n,z/n);}
    public static FluidVector operator +(FluidVector a,FluidVector b)=>new FluidVector(a.x+b.x,a.y+b.y,a.z+b.z);
    public static FluidVector operator *(FluidVector a,double n)=>new FluidVector(a.x*n,a.y*n,a.z*n);
}

public static class BetaFluidPhysics
{
    public static FluidVector Current(Func<WaterPosition,int> sample,Func<WaterPosition,bool> solidFace,WaterPosition p)
    {
        int raw=sample(p);if(raw<0)return new FluidVector();
        int level=raw>=8?0:raw;var flow=new FluidVector();
        for(int d=0;d<4;d++)
        {
            int dx=d==0?-1:d==2?1:0,dz=d==1?-1:d==3?1:0;
            var q=p.Offset(dx,0,dz);int other=sample(q),difference=0;
            if(other>=0)difference=(other>=8?0:other)-level;
            else if(other==WaterSimulation.Air)
            {other=sample(q.Offset(0,-1,0));if(other>=0)difference=(other>=8?0:other)-(level-8);}
            flow.x+=dx*difference;flow.z+=dz*difference;
        }
        if(raw>=8)
            for(int d=0;d<4;d++)
            {
                int dx=d==0?-1:d==2?1:0,dz=d==1?-1:d==3?1:0;
                if(!solidFace(p.Offset(dx,0,dz))&&!solidFace(p.Offset(dx,1,dz)))continue;
                flow=flow.Normalized();flow.y-=6;break;
            }
        return flow.Normalized();
    }

    public static float EyeSurface(int y,int level)=>y+1-(level>=8?0:level)/9f;

    public static FluidVector Accelerate(FluidVector motion,double strafe,double forward,FluidVector right,FluidVector facing,bool jump,FluidVector current,bool lava)
    {
        if(!lava)motion+=current.Normalized()*.014;
        if(jump)motion.y+=.04;
        strafe*=.98;forward*=.98;
        double length=Math.Sqrt(strafe*strafe+forward*forward);
        if(length>=.01)
        {
            double factor=.02/Math.Max(1,length);
            motion+=right*(strafe*factor)+facing*(forward*factor);
        }
        return motion;
    }
    public static FluidVector Drag(FluidVector motion,bool lava)
    {motion=motion*(lava?.5:.8);motion.y-=.02;return motion;}
}

// Damage resistance uses Beta's larger-hit-difference rule, not one hit per
// rendered frame. Air instantly refills on surfacing, as in this version.
[Serializable]
public sealed class FluidHazards
{
    public int air=300,fireTicks;
    private int resistance,lastDamage;
    private int Hit(int amount)
    {
        if(resistance>10){if(amount<=lastDamage)return 0;int delta=amount-lastDamage;lastDamage=amount;return delta;}
        lastDamage=amount;resistance=20;return amount;
    }
    public int Tick(bool water,bool headUnderwater,bool lava,bool fire,bool immune)
    {
        if(resistance>0)resistance--;
        if(immune){air=300;fireTicks=0;return 0;}
        int damage=0;
        if(water)fireTicks=0;
        else if(fireTicks>0){if(fireTicks%20==0)damage+=Hit(1);fireTicks--;}
        if(lava){damage+=Hit(4);fireTicks=600;}
        else if(fire&&!water){damage+=Hit(1);if(fireTicks<=0)fireTicks=300;}
        if(headUnderwater)
        {if(--air==-20){air=0;damage+=Hit(2);}fireTicks=0;}
        else air=300;
        return damage;
    }
}
