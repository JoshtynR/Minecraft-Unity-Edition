using UnityEngine;

public static class FluidWorldQueries
{
    public static int Sample(World world,WaterPosition p,BlockType liquid)
    {
        var b=world.GetBlock(new Vector3Int(p.x,p.y,p.z));
        if(b==null||b.type==BlockType.Nothing)return WaterSimulation.Solid;
        if(b.type==liquid)return b.waterLevel;
        return World.FluidHasSolidMaterial(b.type)?WaterSimulation.Solid:WaterSimulation.Air;
    }
    public static bool SolidFace(World world,WaterPosition p)
    {
        var b=world.GetBlock(new Vector3Int(p.x,p.y,p.z));
        return b==null||b.type==BlockType.Nothing||(b.type!=BlockType.Ice&&SpecialBlockGeometry.NormalCube(b.type));
    }
    public static BlockType AtEye(World world,Vector3 eye)
    {
        if(world==null)return BlockType.Air;
        var p=Vector3Int.FloorToInt(eye);var b=world.GetBlock(p);
        if(b==null||(b.type!=BlockType.Water&&b.type!=BlockType.Lava))return BlockType.Air;
        return eye.y<BetaFluidPhysics.EyeSurface(p.y,b.waterLevel)?b.type:BlockType.Air;
    }
    public static bool Touches(World world,Vector3 feet,float radius,float height,BlockType type,out FluidVector current)
    {
        current=new FluidVector();if(world==null)return false;
        float inset=type==BlockType.Lava?.1f:.001f;
        float minY=feet.y+(type==BlockType.Fire?.001f:.401f);
        float maxY=feet.y+height-(type==BlockType.Fire?.001f:.401f);
        if(maxY<minY)return false;
        int x0=Mathf.FloorToInt(feet.x-radius+inset),x1=Mathf.FloorToInt(feet.x+radius-inset);
        int z0=Mathf.FloorToInt(feet.z-radius+inset),z1=Mathf.FloorToInt(feet.z+radius-inset);
        bool found=false;
        for(int x=x0;x<=x1;x++)for(int y=Mathf.FloorToInt(minY);y<=Mathf.FloorToInt(maxY);y++)for(int z=z0;z<=z1;z++)
        {
            var p=new WaterPosition(x,y,z);var b=world.GetBlock(new Vector3Int(x,y,z));
            if(b==null||b.type!=type)continue;
            found=true;
            if(type==BlockType.Water)current+=BetaFluidPhysics.Current(q=>Sample(world,q,type),q=>SolidFace(world,q),p);
        }
        current=current.Normalized();return found;
    }
}
