using System;
using System.Collections.Generic;

// Beta 1.8-inspired architecture on Beta 1.7.3 land. Layout/placement are an
// independent deterministic adaptation, not vanilla 1.8 structure seed parity.
public struct BetaVillageGround
{
    public int height; // first air, including the surface of water
    public BlockType top;
    public BetaBiomeType biome;
}
public interface IBetaVillageTerrain { BetaVillageGround Ground(int x,int z); }
public enum BetaVillageKind { Well,Road,House,Hut,LargeHouse,Library,Church,Blacksmith,Farm,Lamp }

public sealed class BetaVillagePiece
{
    public readonly BetaVillageKind kind;
    public readonly int x,z,width,depth,rotation,floor;
    private static readonly byte[] TorchFacing={3,1,4,2},LadderFacing={2,4,3,5},FurnaceFacing={2,5,3,4};
    private readonly int[] heights;
    private readonly BlockType[] surfaces;
    internal BetaVillagePiece(BetaVillageKind kind,int x,int z,int width,int depth,int rotation,int floor,int[] heights,BlockType[] surfaces)
    {this.kind=kind;this.x=x;this.z=z;this.width=width;this.depth=depth;this.rotation=rotation;this.floor=floor;this.heights=heights;this.surfaces=surfaces;}
    public void Position(int u,int v,out int wx,out int wz)
    {
        wx=x+(rotation==0?u:rotation==1?-v:rotation==2?-u:v);
        wz=z+(rotation==0?v:rotation==1?u:rotation==2?-v:-u);
    }
    private int Column(int u,int v)=>(u+1)*(depth+3)+v+2;
    private int Height(int u,int v)=>heights[Column(u,v)];
    private BlockType Surface(int u,int v)=>surfaces[Column(u,v)];
    public void Bounds(out int minX,out int minZ,out int maxX,out int maxZ)
    {
        Position(-1,-2,out int a,out int b);Position(width,depth,out int c,out int d);
        minX=Math.Min(a,c);minZ=Math.Min(b,d);maxX=Math.Max(a,c);maxZ=Math.Max(b,d);
    }
    public bool TouchesChunk(int cx,int cz)
    {Bounds(out int a,out int b,out int c,out int d);return a<=cx*16+15&&c>=cx*16&&b<=cz*16+15&&d>=cz*16;}
    // All emissions, including cleared air, are clipped before entering storage.
    // Metadata below is already reflected for Unity's -X coordinate boundary.
    public void Emit(int cx,int cz,Action<int,int,int,BlockType,byte> write)
    {
        Action<int,int,int,BlockType,byte> put=(u,y,v,type,state)=>
        {
            if(y<=0||y>=128)return;Position(u,v,out int wx,out int wz);
            if(wx<cx*16||wx>=cx*16+16||wz<cz*16||wz>=cz*16+16)return;
            write(wx-cx*16,y,wz-cz*16,type,state);
        };
        Action<int,int,int,BlockType> block=(u,y,v,type)=>put(u,y,v,type,0);
        if(kind==BetaVillageKind.Road)
        {
            for(int v=0;v<depth;v++)for(int u=0;u<width;u++)
            {
                int y=Height(1,v)-1;
                for(int yy=Math.Min(y-1,Height(u,v)-1);yy<y;yy++)block(u,yy,v,BlockType.Cobblestone);
                block(u,y-1,v,BlockType.Cobblestone);
                block(u,y,v,Surface(u,v)==BlockType.Water?BlockType.WoodenPlanks:BlockType.Gravel);
                for(int yy=y+1;yy<=y+3;yy++)block(u,yy,v,BlockType.Air);
            }
            return;
        }
        int clear=kind==BetaVillageKind.Church?13:kind==BetaVillageKind.Lamp?6:10;
        for(int u=-1;u<=width;u++)for(int v=-2;v<=depth;v++)
        {
            // Clear the footprint/roof envelope, never an entire chunk surface.
            for(int y=floor+1;y<=floor+clear;y++)block(u,y,v,BlockType.Air);
            if(u<0||u>=width||v<0||v>=depth)continue;
            for(int y=Math.Min(floor-1,Height(u,v)-1);y<floor;y++)block(u,y,v,BlockType.Cobblestone);
            block(u,floor,v,BlockType.Cobblestone);
        }
        if(kind==BetaVillageKind.Lamp)
        {
            for(int y=1;y<=3;y++)block(0,floor+y,0,BlockType.Fence);
            block(0,floor+4,0,BlockType.BlackWool);put(0,floor+5,0,BlockType.Torch,5);return;
        }
        if(kind==BetaVillageKind.Well)
        {
            for(int u=1;u<width-1;u++)for(int v=1;v<depth-1;v++)
            {block(u,floor-4,v,BlockType.Cobblestone);for(int y=-3;y<=0;y++)block(u,floor+y,v,BlockType.Water);}
            for(int u=0;u<width;u++)for(int v=0;v<depth;v++)
            {
                if(u==0||u==width-1||v==0||v==depth-1)for(int y=-4;y<0;y++)block(u,floor+y,v,BlockType.Cobblestone);
                if((u==0||u==width-1)&&(v==0||v==depth-1))for(int y=1;y<=3;y++)block(u,floor+y,v,BlockType.Fence);
                block(u,floor+4,v,BlockType.Cobblestone);
            }
            return;
        }
        if(kind==BetaVillageKind.Farm)
        {
            for(int u=0;u<width;u++)for(int v=0;v<depth;v++)
            {
                block(u,floor-1,v,BlockType.Dirt);
                if(u==0||u==width-1||v==0||v==depth-1)block(u,floor,v,BlockType.Log);
                else if(u==width/2)block(u,floor,v,BlockType.Water);
                else {put(u,floor,v,BlockType.MoistFarmland,7);put(u,floor+1,v,BlockType.WheatCrop0,(byte)(2+(u*3+v*5+rotation)%6));}
            }
            return;
        }
        bool church=kind==BetaVillageKind.Church,smith=kind==BetaVillageKind.Blacksmith;
        BlockType wall=church||smith?BlockType.Cobblestone:BlockType.WoodenPlanks;
        for(int u=0;u<width;u++)for(int v=0;v<depth;v++)
        {
            bool edge=u==0||u==width-1||v==0||v==depth-1;
            for(int y=1;y<=3;y++)if(edge)block(u,floor+y,v,wall);
            if(!church&&!smith&&(u==0||u==width-1)&&(v==0||v==depth-1))
                for(int y=1;y<=3;y++)block(u,floor+y,v,BlockType.Log);
        }
        for(int v=2;v<depth-2;v+=3){block(0,floor+2,v,BlockType.Glass);block(width-1,floor+2,v,BlockType.Glass);}
        for(int u=2;u<width-2;u+=3)block(u,floor+2,depth-1,BlockType.Glass);
        int door=width/2;
        put(door,floor+1,0,BlockType.WoodenDoor,(byte)((rotation+1)&3));
        put(door,floor+2,0,BlockType.WoodenDoor,(byte)(((rotation+1)&3)|8));
        put(door,floor+3,1,BlockType.Torch,TorchFacing[rotation]);
        // A short graded doorstep joins the gravel street without a tall lip.
        for(int v=-2;v<0;v++)
        {
            int step=Math.Max(Height(door,v)-1,floor+v);
            for(int y=Math.Min(step-1,Height(door,v)-1);y<step;y++)block(door,y,v,BlockType.Cobblestone);
            put(door,step,v,BlockType.CobblestoneStairs,StairState(0,1));
            for(int y=step+1;y<=floor+3;y++)block(door,y,v,BlockType.Air);
        }
        if(church)
        {
            for(int u=0;u<width;u++)for(int v=0;v<depth;v++)block(u,floor+4,v,BlockType.Cobblestone);
            for(int u=0;u<width;u++)for(int v=depth-5;v<depth;v++)
            {
                bool edge=u==0||u==width-1||v==depth-5||v==depth-1;
                for(int y=5;y<=10;y++)if(edge||y==6||y==10)block(u,floor+y,v,BlockType.Cobblestone);
                if(edge&&(u%2==0||v%2==0))block(u,floor+11,v,BlockType.Cobblestone);
            }
            for(int y=1;y<=9;y++)put(1,floor+y,depth-2,BlockType.Ladder,LadderFacing[rotation]);
            block(width/2,floor+8,depth-1,BlockType.Glass);block(0,floor+8,depth-3,BlockType.Glass);block(width-1,floor+8,depth-3,BlockType.Glass);
        }
        else if(smith)
        {
            for(int u=-1;u<=width;u++)for(int v=-1;v<=depth;v++)block(u,floor+4,v,BlockType.StoneSlab);
            for(int u=1;u<=3;u++)for(int v=2;v<=4;v++)
            {
                block(u,floor+1,v,BlockType.Cobblestone);
                if(u==2&&v==3)block(u,floor+1,v,BlockType.Lava);
            }
            for(int y=1;y<=2;y++)put(width-2,floor+y,depth-2,BlockType.Furnace,FurnaceFacing[rotation]);
            for(int y=4;y<=7;y++)block(2,floor+y,3,BlockType.Cobblestone);
            // No modern loot table or village residents are introduced here.
        }
        else if(kind==BetaVillageKind.Hut)
        {
            for(int u=-1;u<=width;u++)for(int v=-1;v<=depth;v++)block(u,floor+4,v,BlockType.WoodenSlab);
        }
        else
        {
            for(int u=-1;u<=width;u++)
            {
                int rise=Math.Min(u+1,width-u),roof=floor+4+rise;
                for(int v=-1;v<=depth;v++)
                    if(u==width/2)block(u,roof,v,BlockType.WoodenPlanks);
                    else put(u,roof,v,BlockType.WoodenStairs,StairState(u<width/2?1:-1,0));
                if(u>=0&&u<width)for(int y=floor+4;y<roof;y++)
                {block(u,y,0,BlockType.WoodenPlanks);block(u,y,depth-1,BlockType.WoodenPlanks);}
            }
        }
        if(kind==BetaVillageKind.Library)
        {
            for(int v=2;v<depth-1;v++)for(int y=1;y<=2;y++){block(1,floor+y,v,BlockType.Bookshelf);block(width-2,floor+y,v,BlockType.Bookshelf);}
            block(width/2,floor+1,depth-2,BlockType.CraftingTable);
        }
        else if(!church&&!smith)
        {
            block(1,floor+1,depth-2,BlockType.CraftingTable);
            put(width-2,floor+1,depth-2,BlockType.WoodenStairs,StairState(0,1));
        }
    }
    private byte StairState(int u,int v)
    {
        Position(u,v,out int a,out int b);int dx=-(a-x),dz=b-z;
        return dx>0?(byte)0:dx<0?(byte)1:dz>0?(byte)2:(byte)3;
    }
}

public sealed class Beta18Villages
{
    public const int Spacing=32,Separation=8,Radius=64;
    private readonly long seed;
    private readonly IBetaVillageTerrain terrain;
    private readonly object sync=new object();
    private readonly Dictionary<long,BetaVillagePiece[]> plans=new Dictionary<long,BetaVillagePiece[]>();
    private readonly Queue<long> order=new Queue<long>();
    public Beta18Villages(long seed,IBetaVillageTerrain terrain){this.seed=seed;this.terrain=terrain;}
    public static int FloorDiv(int value,int divisor){int q=value/divisor;return value%divisor<0?q-1:q;}
    public void Candidate(int rx,int rz,out int x,out int z)
    {
        var r=new JavaRandom(unchecked(seed+(long)rx*341873128712L+(long)rz*132897987541L+10387312));
        x=unchecked((rx*Spacing+r.NextInt(Spacing-Separation))*16+8);
        z=unchecked((rz*Spacing+r.NextInt(Spacing-Separation))*16+8);
    }
    public BetaVillagePiece[] ForChunk(int cx,int cz)
    {
        var result=new List<BetaVillagePiece>();
        if(Math.Abs((long)cx*16)>30000000||Math.Abs((long)cz*16)>30000000)return result.ToArray();
        lock(sync)
        {
            int minX=FloorDiv(cx*16-Radius,Spacing*16),maxX=FloorDiv(cx*16+15+Radius,Spacing*16);
            int minZ=FloorDiv(cz*16-Radius,Spacing*16),maxZ=FloorDiv(cz*16+15+Radius,Spacing*16);
            for(int rx=minX;rx<=maxX;rx++)for(int rz=minZ;rz<=maxZ;rz++)
            {
                Candidate(rx,rz,out int x,out int z);
                if(x<cx*16-Radius||x>cx*16+15+Radius||z<cz*16-Radius||z>cz*16+15+Radius)continue;
                long key=unchecked(((long)rx<<32)|(uint)rz);
                if(!plans.TryGetValue(key,out var pieces))
                {
                    pieces=Build(x,z);if(plans.Count>=64)plans.Remove(order.Dequeue());
                    plans.Add(key,pieces);order.Enqueue(key);
                }
                foreach(var piece in pieces)if(piece.TouchesChunk(cx,cz))result.Add(piece);
            }
        }
        return result.ToArray();
    }
    private static bool Biome(BetaBiomeType b)=>b==BetaBiomeType.Plains||b==BetaBiomeType.Desert||b==BetaBiomeType.Savanna||b==BetaBiomeType.Shrubland;
    private BetaVillagePiece Make(BetaVillageKind kind,int x,int z,int w,int d,int rotation,int rootFloor)
    {
        int[] heights=new int[(w+2)*(d+3)];var surfaces=new BlockType[heights.Length];
        // Probe every column once. Emission never samples mutable loaded chunks.
        var probe=new BetaVillagePiece(kind,x,z,w,d,rotation,0,heights,surfaces);
        int min=128,max=0,total=0,outerMin=128,outerMax=0;
        for(int u=-1;u<=w;u++)for(int v=-2;v<=d;v++)
        {
            probe.Position(u,v,out int wx,out int wz);var g=terrain.Ground(wx,wz);int i=(u+1)*(d+3)+v+2;
            heights[i]=g.height;surfaces[i]=g.top;
            if(g.height<62||g.height>108||g.top==BlockType.Lava)return null;
            outerMin=Math.Min(outerMin,g.height);outerMax=Math.Max(outerMax,g.height);
            if(u<0||u>=w||v<0||v>=d)continue;
            if(kind!=BetaVillageKind.Road&&g.top==BlockType.Water)return null;
            min=Math.Min(min,g.height);max=Math.Max(max,g.height);total+=g.height;
        }
        int floor=total/(w*d)-1;
        if(kind==BetaVillageKind.Road)
        {
            int previous=rootFloor+1;
            for(int v=0;v<d;v++)
            {int h=heights[2*(d+3)+v+2];if(Math.Abs(h-previous)>1||Math.Abs(h-1-rootFloor)>7)return null;
                for(int u=0;u<w;u++)if(Math.Abs(heights[(u+1)*(d+3)+v+2]-h)>2)return null;previous=h;}
            if(max-min>10)return null;
        }
        else if(max-min>6||outerMax-outerMin>8||Math.Abs(floor-rootFloor)>7||floor+14>=128)return null;
        if(kind!=BetaVillageKind.Well&&kind!=BetaVillageKind.Road&&kind!=BetaVillageKind.Farm&&kind!=BetaVillageKind.Lamp&&
            (Math.Abs(heights[(w/2+1)*(d+3)+1]-1-floor)>2||Math.Abs(heights[(w/2+1)*(d+3)]-1-floor)>2))return null;
        return new BetaVillagePiece(kind,x,z,w,d,rotation,floor,heights,surfaces);
    }
    private static bool Overlap(BetaVillagePiece a,BetaVillagePiece b)
    {
        a.Bounds(out int ax,out int az,out int ax2,out int az2);b.Bounds(out int bx,out int bz,out int bx2,out int bz2);
        return ax<=bx2&&ax2>=bx&&az<=bz2&&az2>=bz;
    }
    private BetaVillagePiece[] Build(int x,int z)
    {
        var ground=terrain.Ground(x,z);
        if(!Biome(ground.biome)||ground.top==BlockType.Water||ground.height<63||ground.height>106)return new BetaVillagePiece[0];
        var r=new JavaRandom(unchecked(seed^(long)x*341873128712L^(long)z*132897987541L^0x56494C4C414745L));
        var well=Make(BetaVillageKind.Well,x-2,z-2,5,5,0,ground.height-1);
        if(well==null)return new BetaVillagePiece[0];
        var pieces=new List<BetaVillagePiece>{well};var buildings=new List<BetaVillagePiece>();int count=0;
        for(int arm=0;arm<4;arm++)
        {
            int dx=arm==1?1:arm==3?-1:0,dz=arm==0?1:arm==2?-1:0,length=24+r.NextInt(17);
            // Roads are width 3 in their local U direction, extending along V.
            int rotation=arm==0?0:arm==1?3:arm==2?2:1;
            int ux=rotation==0?1:rotation==2?-1:0,uz=rotation==1?1:rotation==3?-1:0;
            BetaVillagePiece road=null;
            for(int len=length;len>=12;len-=4)
            {road=Make(BetaVillageKind.Road,x+dx*4-ux,z+dz*4-uz,3,len,rotation,well.floor);if(road!=null)break;}
            if(road==null)continue;pieces.Add(road);
            for(int distance=9;distance<road.depth+1;distance+=13)for(int side=-1;side<=1;side+=2)
            {
                int roll=r.NextInt(100);
                var kind=count==0?BetaVillageKind.Farm:roll<24?BetaVillageKind.Farm:roll<48?BetaVillageKind.House:roll<62?BetaVillageKind.Hut:
                    roll<75?BetaVillageKind.LargeHouse:roll<85?BetaVillageKind.Library:roll<92?BetaVillageKind.Church:BetaVillageKind.Blacksmith;
                int w=kind==BetaVillageKind.Hut?5:kind==BetaVillageKind.Church?5:kind==BetaVillageKind.LargeHouse||kind==BetaVillageKind.Library?9:kind==BetaVillageKind.Blacksmith?10:7;
                int d=kind==BetaVillageKind.Farm?9:kind==BetaVillageKind.Church?9:kind==BetaVillageKind.LargeHouse?12:kind==BetaVillageKind.Hut?5:7;
                int nx=-dz*side,nz=dx*side,rot=nz==1?0:nx==-1?1:nz==-1?2:3;
                int rightX=rot==0?1:rot==2?-1:0,rightZ=rot==1?1:rot==3?-1:0;
                var building=Make(kind,x+dx*distance+nx*4-rightX*(w/2),z+dz*distance+nz*4-rightZ*(w/2),w,d,rot,well.floor);
                if(building==null)continue;
                bool overlap=false;foreach(var other in buildings)if(Overlap(building,other)){overlap=true;break;}
                if(overlap||Overlap(building,well))continue;
                buildings.Add(building);pieces.Add(building);count++;
            }
            var lamp=Make(BetaVillageKind.Lamp,x+dx*5-dz*3,z+dz*5+dx*3,1,1,0,well.floor);
            if(lamp!=null)
            {bool overlaps=false;foreach(var building in buildings)if(Overlap(lamp,building)){overlaps=true;break;}if(!overlaps)pieces.Add(lamp);}
        }
        return count>=3?pieces.ToArray():new BetaVillagePiece[0];
    }
}
