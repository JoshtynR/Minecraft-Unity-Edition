using System;

// Canonical Beta coordinates. The host owns storage, light, and tile records.
public interface IBetaPopulationWorld : IBetaTreeWorld
{
    int Height(int x, int z);
    int Light(int x, int y, int z);
    int SkyLight(int x, int y, int z);
    bool SeesSky(int x, int y, int z);
    void Metadata(int x, int y, int z, int value);
    void Loot(int x, int y, int z, int slot, int item, int count, int damage);
    void Spawner(int x, int y, int z, string mob);
    void Spring(int x, int y, int z, BlockType liquid, JavaRandom random);
}

public static class BetaPopulationFeatures
{
    public static bool Liquid(BlockType b) => b == BlockType.Water || b == BlockType.Lava;
    // Canonical Beta opacity, independent of user render/data overrides.
    public static int Opacity(BlockType b)
    {
        switch (b)
        {
            case BlockType.Air:
            case BlockType.Glass:
            case BlockType.OakSapling:
            case BlockType.Bed:
            case BlockType.PoweredRail:
            case BlockType.DetectorRail:
            case BlockType.StickyPiston:
            case BlockType.TallGrass:
            case BlockType.DeadBush:
            case BlockType.Piston:
            case BlockType.PistonHead:
            case BlockType.MovingPiston:
            case BlockType.Dandelion:
            case BlockType.Rose:
            case BlockType.BrownMushroom:
            case BlockType.RedMushroom:
            case BlockType.Torch:
            case BlockType.Fire:
            case BlockType.MonsterSpawner:
            case BlockType.RedstoneWire:
            case BlockType.WheatCrop0:
            case BlockType.StandingSign:
            case BlockType.WoodenDoor:
            case BlockType.Ladder:
            case BlockType.Rail:
            case BlockType.WallSign:
            case BlockType.Lever:
            case BlockType.StonePressurePlate:
            case BlockType.IronDoor:
            case BlockType.WoodenPressurePlate:
            case BlockType.RedstoneTorchOff:
            case BlockType.RedstoneTorch:
            case BlockType.StoneButton:
            case BlockType.SnowLayer:
            case BlockType.Cactus:
            case BlockType.SugarCane:
            case BlockType.Fence:
            case BlockType.NetherPortal:
            case BlockType.Cake:
            case BlockType.RedstoneRepeater:
            case BlockType.PoweredRepeater:
            case BlockType.Trapdoor:
            case BlockType.SpruceSapling:
            case BlockType.BirchSapling:
            case BlockType.DryTallGrass:
            case BlockType.Fern:
            case BlockType.WheatCrop1:
            case BlockType.WheatCrop2:
            case BlockType.WheatCrop3:
            case BlockType.WheatCrop4:
            case BlockType.WheatCrop5:
            case BlockType.WheatCrop6:
            case BlockType.WheatCrop7:
                return 0;
            case BlockType.Leaves:
            case BlockType.SpruceLeaves:
            case BlockType.BirchLeaves:
            case BlockType.Cobweb:
                return 1;
            case BlockType.Water:
            case BlockType.Ice:
                return 3;
            default: return 15; // Includes unavailable data, never a light source.
        }
    }
    public static bool Solid(BlockType b)
    {
        if (b == BlockType.Nothing || b == BlockType.Air || Liquid(b) || b == BlockType.Fire || b == BlockType.SnowLayer || b == BlockType.NetherPortal) return false;
        BetaBlockCatalog.Entry e;
        return !(BetaBlockCatalog.TryGet(b, out e) && e.CornerPass);
    }
    public static bool Opaque(BlockType b)
    {
        if (!Solid(b) || b == BlockType.Glass || b == BlockType.Ice || Beta173Trees.IsLeaves(b)) return false;
        return b != BlockType.WoodenStairs && b != BlockType.CobblestoneStairs && b != BlockType.Cactus &&
            b != BlockType.Fence && b != BlockType.MonsterSpawner && b != BlockType.Bed && b != BlockType.Cake &&
            b != BlockType.Farmland && b != BlockType.MoistFarmland &&
            b != BlockType.WoodenSlab && b != BlockType.StoneSlab && b != BlockType.SandstoneSlab && b != BlockType.CobblestoneSlab;
    }
    public static bool Vein(IBetaPopulationWorld w, JavaRandom r, int x, int y, int z, BlockType type, int size, bool clay = false)
    {
        if (clay && w.Get(x,y,z) != BlockType.Water) return false;
        float angle = r.NextFloat() * 3.1415927f;
        double ax = (float)(x+8) + BetaMathHelper.Sin(angle) * size / 8f;
        double bx = (float)(x+8) - BetaMathHelper.Sin(angle) * size / 8f;
        double az = (float)(z+8) + BetaMathHelper.Cos(angle) * size / 8f;
        double bz = (float)(z+8) - BetaMathHelper.Cos(angle) * size / 8f;
        double ay = y + r.NextInt(3) + 2, by = y + r.NextInt(3) + 2;
        for(int i=0;i<=size;i++)
        {
            double cx=ax+(bx-ax)*i/size, cy=ay+(by-ay)*i/size, cz=az+(bz-az)*i/size;
            double radius=r.NextDouble()*size/16.0;
            double diameter=(BetaMathHelper.Sin((float)i*3.1415927f/size)+1f)*radius+1.0;
            for(int xx=BetaMathHelper.Floor(cx-diameter/2);xx<=BetaMathHelper.Floor(cx+diameter/2);xx++)
            for(int yy=BetaMathHelper.Floor(cy-diameter/2);yy<=BetaMathHelper.Floor(cy+diameter/2);yy++)
            for(int zz=BetaMathHelper.Floor(cz-diameter/2);zz<=BetaMathHelper.Floor(cz+diameter/2);zz++)
            {
                double dx=(xx+.5-cx)/(diameter/2),dy=(yy+.5-cy)/(diameter/2),dz=(zz+.5-cz)/(diameter/2);
                if(dx*dx+dy*dy+dz*dz<1 && w.Get(xx,yy,zz)==(clay?BlockType.Sand:BlockType.Stone)) w.Set(xx,yy,zz,type);
            }
        }
        return true;
    }
    public static bool PlantStays(IBetaPopulationWorld w,BlockType type,int x,int y,int z)
    {
        if (y<0 || y>=128) return false;
        var below=w.Get(x,y-1,z);
        if(type==BlockType.BrownMushroom||type==BlockType.RedMushroom) return w.Light(x,y,z)<13&&Opaque(below);
        bool soil=type==BlockType.DeadBush?below==BlockType.Sand:
            below==BlockType.Grass||below==BlockType.Dirt||below==BlockType.Farmland||below==BlockType.MoistFarmland;
        return soil&&(w.SeesSky(x,y,z)||w.Light(x,y,z)>=8);
    }
    public static void Plants(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z,BlockType type)
    {
        bool grass=type==BlockType.TallGrass||type==BlockType.Fern||type==BlockType.DeadBush;
        if(grass)while(y>0&&(w.Get(x,y,z)==BlockType.Air||Beta173Trees.IsLeaves(w.Get(x,y,z))))y--;
        int attempts=type==BlockType.DeadBush?4:grass?128:64;
        for(int i=0;i<attempts;i++)
        {
            int xx=x+r.NextInt(8)-r.NextInt(8),yy=y+r.NextInt(4)-r.NextInt(4),zz=z+r.NextInt(8)-r.NextInt(8);
            if(w.Get(xx,yy,zz)==BlockType.Air&&PlantStays(w,type,xx,yy,zz))w.Set(xx,yy,zz,type);
        }
    }
    private static bool WaterBeside(IBetaPopulationWorld w,int x,int y,int z) =>
        w.Get(x-1,y,z)==BlockType.Water||w.Get(x+1,y,z)==BlockType.Water||w.Get(x,y,z-1)==BlockType.Water||w.Get(x,y,z+1)==BlockType.Water;
    public static void Reeds(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z)
    {
        for(int i=0;i<20;i++)
        {
            int xx=x+r.NextInt(4)-r.NextInt(4),zz=z+r.NextInt(4)-r.NextInt(4);
            if(w.Get(xx,y,zz)!=BlockType.Air||!WaterBeside(w,xx,y-1,zz))continue;
            int height=2+r.NextInt(r.NextInt(3)+1);
            for(int j=0;j<height;j++)
            {
                var below=w.Get(xx,y+j-1,zz);
                if(below==BlockType.SugarCane||((below==BlockType.Grass||below==BlockType.Dirt)&&WaterBeside(w,xx,y+j-1,zz)))w.Set(xx,y+j,zz,BlockType.SugarCane);
            }
        }
    }
    public static void Cacti(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z)
    {
        for(int i=0;i<10;i++)
        {
            int xx=x+r.NextInt(8)-r.NextInt(8),yy=y+r.NextInt(4)-r.NextInt(4),zz=z+r.NextInt(8)-r.NextInt(8);
            if(w.Get(xx,yy,zz)!=BlockType.Air)continue;
            int height=1+r.NextInt(r.NextInt(3)+1);
            for(int j=0;j<height;j++)
            {
                var below=w.Get(xx,yy+j-1,zz);
                if((below==BlockType.Sand||below==BlockType.Cactus)&&!Solid(w.Get(xx-1,yy+j,zz))&&!Solid(w.Get(xx+1,yy+j,zz))&&
                    !Solid(w.Get(xx,yy+j,zz-1))&&!Solid(w.Get(xx,yy+j,zz+1)))w.Set(xx,yy+j,zz,BlockType.Cactus);
            }
        }
    }
    public static void Pumpkins(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z)
    {
        for(int i=0;i<64;i++)
        {
            int xx=x+r.NextInt(8)-r.NextInt(8),yy=y+r.NextInt(4)-r.NextInt(4),zz=z+r.NextInt(8)-r.NextInt(8);
            if(w.Get(xx,yy,zz)==BlockType.Air&&w.Get(xx,yy-1,zz)==BlockType.Grass)
            {w.Set(xx,yy,zz,BlockType.Pumpkin);w.Metadata(xx,yy,zz,r.NextInt(4));}
        }
    }
    public static bool Spring(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z,BlockType liquid)
    {
        if(w.Get(x,y+1,z)!=BlockType.Stone||w.Get(x,y-1,z)!=BlockType.Stone)return false;
        var center=w.Get(x,y,z);if(center!=BlockType.Air&&center!=BlockType.Stone)return false;
        int stone=0,air=0;
        var side=new[]{w.Get(x-1,y,z),w.Get(x+1,y,z),w.Get(x,y,z-1),w.Get(x,y,z+1)};
        foreach(var b in side){if(b==BlockType.Stone)stone++;if(b==BlockType.Air)air++;}
        if(stone==3&&air==1)w.Spring(x,y,z,liquid,r);
        return true;
    }
    public static bool Dungeon(IBetaPopulationWorld w,JavaRandom r,int x,int y,int z)
    {
        int sx=r.NextInt(2)+2,sz=r.NextInt(2)+2,open=0;
        for(int xx=x-sx-1;xx<=x+sx+1;xx++)
        for(int yy=y-1;yy<=y+4;yy++)
        for(int zz=z-sz-1;zz<=z+sz+1;zz++)
        {
            if((yy==y-1||yy==y+4)&&!Solid(w.Get(xx,yy,zz)))return false;
            if((xx==x-sx-1||xx==x+sx+1||zz==z-sz-1||zz==z+sz+1)&&yy==y&&w.Get(xx,yy,zz)==BlockType.Air&&w.Get(xx,yy+1,zz)==BlockType.Air)open++;
        }
        if(open<1||open>5)return false;
        for(int xx=x-sx-1;xx<=x+sx+1;xx++)
        for(int yy=y+3;yy>=y-1;yy--)
        for(int zz=z-sz-1;zz<=z+sz+1;zz++)
        {
            if(xx!=x-sx-1&&yy!=y-1&&zz!=z-sz-1&&xx!=x+sx+1&&yy!=y+4&&zz!=z+sz+1)w.Set(xx,yy,zz,BlockType.Air);
            else if(yy>=0&&!Solid(w.Get(xx,yy-1,zz)))w.Set(xx,yy,zz,BlockType.Air);
            else if(Solid(w.Get(xx,yy,zz)))w.Set(xx,yy,zz,yy==y-1&&r.NextInt(4)!=0?BlockType.MossyCobblestone:BlockType.Cobblestone);
        }
        for(int chest=0;chest<2;chest++)
        for(int attempt=0;attempt<3;attempt++)
        {
            int xx=x+r.NextInt(sx*2+1)-sx,zz=z+r.NextInt(sz*2+1)-sz;
            if(w.Get(xx,y,zz)!=BlockType.Air)continue;
            int walls=(Solid(w.Get(xx-1,y,zz))?1:0)+(Solid(w.Get(xx+1,y,zz))?1:0)+(Solid(w.Get(xx,y,zz-1))?1:0)+(Solid(w.Get(xx,y,zz+1))?1:0);
            if(walls!=1)continue;
            w.Set(xx,y,zz,BlockType.Chest);
            for(int roll=0;roll<8;roll++)
            {
                int item,count,damage;
                if(PickLoot(r,out item,out count,out damage))w.Loot(xx,y,zz,r.NextInt(27),item,count,damage);
            }
            break;
        }
        w.Set(x,y,z,BlockType.MonsterSpawner);
        int mob=r.NextInt(4);w.Spawner(x,y,z,mob==0?"Skeleton":mob==3?"Spider":"Zombie");
        return true;
    }
    private static bool PickLoot(JavaRandom r,out int item,out int count,out int damage)
    {
        item=0;count=1;damage=0;int roll=r.NextInt(11);
        switch(roll)
        {
            case 0:item=329;break;
            case 1:item=265;count=r.NextInt(4)+1;break;
            case 2:item=297;break;
            case 3:item=296;count=r.NextInt(4)+1;break;
            case 4:item=289;count=r.NextInt(4)+1;break;
            case 5:item=287;count=r.NextInt(4)+1;break;
            case 6:item=325;break;
            case 7:if(r.NextInt(100)==0)item=322;break;
            case 8:if(r.NextInt(2)==0){item=331;count=r.NextInt(4)+1;}break;
            case 9:if(r.NextInt(10)==0)item=2256+r.NextInt(2);break;
            case 10:item=351;damage=3;break;
        }
        return item!=0;
    }
}
