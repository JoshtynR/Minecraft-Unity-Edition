using System;
using UnityEngine;

// State lives in the existing blockShape value so pooling/mapping cannot drop it.
// The save/network byte formerly named stairFacing now carries native metadata.
public static class SpecialBlockGeometry
{
    private const int First = 1024;
    private static readonly BlockShape[,] Bounds = new BlockShape[133,16];
    private static readonly BlockShape[] PistonCaps={Box(0,0,0,16,4,16),Box(0,12,0,16,16,16),Box(0,0,0,16,16,4),Box(0,0,12,16,16,16),Box(0,0,0,4,16,16),Box(12,0,0,16,16,16)};
    // Preserve the original extension's asymmetric horizontal collision rods.
    private static readonly BlockShape[] PistonRods={Box(6,4,6,10,16,10),Box(6,0,6,10,12,10),Box(4,6,4,12,10,16),Box(4,6,0,12,10,12),Box(6,4,4,10,12,16),Box(0,6,4,12,10,12)};
    private static readonly BlockShape[] ExtendedBases={Box(0,4,0,16,16,16),Box(0,0,0,16,12,16),Box(0,0,4,16,16,16),Box(0,0,0,16,16,12),Box(4,0,0,16,16,16),Box(0,0,0,12,16,16)};
    public static bool Handles(BlockType t)
    {
        switch(t)
        {
            case BlockType.Torch: case BlockType.RedstoneTorch: case BlockType.RedstoneTorchOff:
            case BlockType.Fence: case BlockType.RedstoneWire: case BlockType.Ladder:
            case BlockType.Rail: case BlockType.PoweredRail: case BlockType.DetectorRail:
            case BlockType.StandingSign: case BlockType.WallSign: case BlockType.WoodenDoor: case BlockType.IronDoor:
            case BlockType.Lever: case BlockType.StoneButton: case BlockType.Trapdoor:
            case BlockType.RedstoneRepeater: case BlockType.PoweredRepeater: case BlockType.Bed:
            case BlockType.Fire: case BlockType.Cobweb: case BlockType.NetherPortal:
            case BlockType.PistonHead: case BlockType.MovingPiston:
            case BlockType.Piston: case BlockType.StickyPiston:
            case BlockType.Furnace: case BlockType.LitFurnace: case BlockType.Dispenser:
            case BlockType.Chest: case BlockType.LockedChest: case BlockType.Pumpkin: case BlockType.JackOLantern:
                return true;
            default: return false;
        }
    }
    public static BlockShapes Shape(BlockType t, byte state)
    {
        if(StairGeometry.IsStair(t)) return StairGeometry.Shape(state);
        return Handles(t) ? (BlockShapes)(First+(int)t*16+(state&15)) : BlockShapesExtensions.ForBlock(t);
    }
    public static byte State(BlockType t, BlockShapes shape)
    {
        if(StairGeometry.IsStair(t)) return StairGeometry.Facing(shape);
        return (int)shape>=First ? (byte)(((int)shape-First)&15) : DefaultState(t);
    }
    public static byte DefaultState(BlockType t)
    {
        if(IsTorch(t)) return 5;
        if(t==BlockType.Ladder || t==BlockType.WallSign || t==BlockType.Furnace || t==BlockType.LitFurnace || t==BlockType.Dispenser) return 2;
        if(t==BlockType.Piston || t==BlockType.StickyPiston || t==BlockType.PistonHead) return 1;
        if(t==BlockType.Lever) return 5;
        if(t==BlockType.StoneButton) return 4;
        return 0;
    }
    public static bool Encoded(BlockShapes shape) => (int)shape>=First;
    public static BlockShape Decode(BlockShapes shape)
    {
        int n=(int)shape-First;
        return Selection((BlockType)(n/16),(byte)(n&15));
    }
    public static bool IsTorch(BlockType t) => t==BlockType.Torch||t==BlockType.RedstoneTorch||t==BlockType.RedstoneTorchOff;
    public static bool IsRail(BlockType t) => t==BlockType.Rail||t==BlockType.PoweredRail||t==BlockType.DetectorRail;
    public static bool FlatShading(BlockType t)=>IsTorch(t)||IsRail(t)||t==BlockType.RedstoneWire||t==BlockType.Ladder||t==BlockType.Fire||t==BlockType.Cobweb;
    public static bool IsDoor(BlockType t) => t==BlockType.WoodenDoor||t==BlockType.IronDoor;
    public static bool NoCollision(BlockType t) => IsTorch(t)||IsRail(t)||t==BlockType.RedstoneWire||t==BlockType.Lever||t==BlockType.StoneButton||t==BlockType.StandingSign||t==BlockType.WallSign||t==BlockType.Fire||t==BlockType.NetherPortal||t==BlockType.Cobweb||t==BlockType.MovingPiston||t==BlockType.RedstoneRepeater||t==BlockType.PoweredRepeater;
    public static bool NonOccluding(BlockType t) => Handles(t) && t!=BlockType.Piston && t!=BlockType.StickyPiston && t!=BlockType.Furnace && t!=BlockType.LitFurnace && t!=BlockType.Dispenser && t!=BlockType.Chest && t!=BlockType.LockedChest && t!=BlockType.Pumpkin && t!=BlockType.JackOLantern;
    public static BlockShape Selection(BlockType t, byte s)
    {
        s=(byte)(s&15);
        var cached=Bounds[(int)t,s]; if(cached!=null)return cached;
        BlockShape b=BlockShapesList.FullBlock;
        if(IsTorch(t))
        {
            b=s==1?Box(0,3.2f,5.6f,4.8f,12.8f,10.4f):s==2?Box(11.2f,3.2f,5.6f,16,12.8f,10.4f):
              s==3?Box(5.6f,3.2f,0,10.4f,12.8f,4.8f):s==4?Box(5.6f,3.2f,11.2f,10.4f,12.8f,16):Box(6.4f,0,6.4f,9.6f,9.6f,9.6f);
        }
        else if(t==BlockType.RedstoneWire) b=Box(0,0,0,16,1,16);
        else if(IsRail(t)) {int rail=t==BlockType.Rail?s:s&7;b=Box(0,0,0,16,rail>=2&&rail<=5?10:2,16);}
        else if(t==BlockType.Ladder || t==BlockType.WallSign)
        {
            float lo=t==BlockType.Ladder?0:4.5f,hi=t==BlockType.Ladder?16:12.5f,d=t==BlockType.Ladder?2:2;
            b=s==2?Box(0,lo,16-d,16,hi,16):s==3?Box(0,lo,0,16,hi,d):s==4?Box(16-d,lo,0,16,hi,16):Box(0,lo,0,d,hi,16);
        }
        else if(t==BlockType.StandingSign) b=Box(4,0,4,12,16,12);
        else if(IsDoor(t))
        {
            int q=(s&4)==0?(s-1)&3:s&3;
            b=q==0?Box(0,0,0,16,16,3):q==1?Box(13,0,0,16,16,16):q==2?Box(0,0,13,16,16,16):Box(0,0,0,3,16,16);
        }
        else if(t==BlockType.Trapdoor)
        {
            b=(s&4)==0?Box(0,0,0,16,3,16):(s&3)==0?Box(0,0,13,16,16,16):(s&3)==1?Box(0,0,0,16,16,3):(s&3)==2?Box(13,0,0,16,16,16):Box(0,0,0,3,16,16);
        }
        else if(t==BlockType.StoneButton || t==BlockType.Lever)
        {
            bool lever=t==BlockType.Lever;float w=lever?3:3,h0=lever?3.2f:6,h1=lever?12.8f:10,d=lever?3.2f:((s&8)!=0?1:2);
            int a=s&7;b=a==1?Box(0,h0,8-w,d,h1,8+w):a==2?Box(16-d,h0,8-w,16,h1,8+w):a==3?Box(8-w,h0,0,8+w,h1,d):a==4?Box(8-w,h0,16-d,8+w,h1,16):Box(4,0,4,12,9.6f,12);
        }
        else if(t==BlockType.RedstoneRepeater||t==BlockType.PoweredRepeater)b=BlockShapesList.Repeater;
        else if(t==BlockType.Bed)b=BlockShapesList.Bed;
        else if(t==BlockType.NetherPortal)b=(s&1)==0?Box(0,0,6,16,16,10):Box(6,0,0,10,16,16);
        else if(t==BlockType.MovingPiston)b=Box(0,0,0,16,16,16);
        else if(t==BlockType.PistonHead)b=PistonCaps[Math.Min(5,s&7)];
        else if((t==BlockType.Piston||t==BlockType.StickyPiston)&&(s&8)!=0)b=ExtendedBases[Math.Min(5,s&7)];
        Bounds[(int)t,s]=b;return b;
    }
    public static readonly BlockShape FenceCollision=Box(0,0,0,16,24,16);
    public static int CollisionCount(BlockType t)=>StairGeometry.IsStair(t)||t==BlockType.PistonHead?2:1;
    public static BlockShape CollisionPart(BlockType t,BlockShapes shape,int part)
    {
        byte s=State(t,shape);
        if(StairGeometry.IsStair(t))return StairGeometry.CollisionPart(s,part);
        if(t==BlockType.PistonHead)return part==0?PistonCaps[Math.Min(5,s&7)]:PistonRods[Math.Min(5,s&7)];
        return BlockShapesExtensions.CollisionShape(t,shape.GetShape());
    }
    public static BlockShape Box(float a,float b,float c,float d,float e,float f)=>new BlockShape(a,b,c,d,e,f);
    // Native horizontal quarter-turn: south, west, north, east.
    public static byte Quarter(Vector3 look)=>(byte)((int)Math.Floor(Math.Atan2(-look.x,look.z)*2/Math.PI+.5)&3);
    public static Vector3Int PartnerOffset(BlockType t,byte s)
    {
        if(IsDoor(t))return new Vector3Int(0,(s&8)==0?1:-1,0);
        int q=s&3,sign=(s&8)==0?1:-1;
        return q==0?new Vector3Int(0,0,sign):q==1?new Vector3Int(-sign,0,0):q==2?new Vector3Int(0,0,-sign):new Vector3Int(sign,0,0);
    }
    public static bool NormalCube(BlockType t)
    {
        if(t==BlockType.Nothing||t==BlockType.Air||t==BlockType.Water||t==BlockType.Lava||t==BlockType.Glass||t==BlockType.Ice||t==BlockType.Leaves||t==BlockType.SpruceLeaves||t==BlockType.BirchLeaves||t==BlockType.Piston||t==BlockType.StickyPiston||t==BlockType.MonsterSpawner)return false;
        return !NonOccluding(t)&&!BlockShapesExtensions.IsPlant(t)&&BlockShapesExtensions.ForBlock(t).GetShape().isFullBlock()&&!StairGeometry.IsStair(t);
    }
    public static bool CanAttach(BlockType t,byte state,Vector3Int pos,Func<Vector3Int,BlockType> get)
    {
        Vector3Int support=new Vector3Int(0,-1,0);
        if(IsTorch(t)||t==BlockType.Lever||t==BlockType.StoneButton)
        {
            int s=state&7;
            support=s==1?new Vector3Int(-1,0,0):s==2?new Vector3Int(1,0,0):s==3?new Vector3Int(0,0,-1):s==4?new Vector3Int(0,0,1):support;
            var block=get(pos+support);
            return NormalCube(block)||(IsTorch(t)&&s==5&&block==BlockType.Fence);
        }
        if(t==BlockType.Ladder||t==BlockType.WallSign)
            support=state==2?new Vector3Int(0,0,1):state==3?new Vector3Int(0,0,-1):state==4?new Vector3Int(1,0,0):new Vector3Int(-1,0,0);
        else if(t==BlockType.Trapdoor)
            support=(state&3)==0?new Vector3Int(0,0,1):(state&3)==1?new Vector3Int(0,0,-1):(state&3)==2?new Vector3Int(1,0,0):new Vector3Int(-1,0,0);
        else if(!IsRail(t)&&t!=BlockType.RedstoneWire&&t!=BlockType.RedstoneRepeater&&t!=BlockType.PoweredRepeater&&t!=BlockType.StandingSign&&!IsDoor(t)&&t!=BlockType.Bed)return true;
        return NormalCube(get(pos+support));
    }
    public static byte RailState(BlockType t,byte current,Vector3Int p,Func<Vector3Int,BlockType> get)
    {
        bool west=RailAt(p,new Vector3Int(-1,0,0),get),east=RailAt(p,new Vector3Int(1,0,0),get);
        bool north=RailAt(p,new Vector3Int(0,0,-1),get),south=RailAt(p,new Vector3Int(0,0,1),get);
        int shape=current&(t==BlockType.Rail?15:7);
        if(!west&&!east&&!north&&!south)shape=shape>=1&&shape<=3?1:0;
        if((north||south)&&!west&&!east)shape=0;
        if((west||east)&&!north&&!south)shape=1;
        if(t==BlockType.Rail)
        {
            if(south&&east&&!north&&!west)shape=6;
            if(south&&west&&!north&&!east)shape=7;
            if(north&&west&&!south&&!east)shape=8;
            if(north&&east&&!south&&!west)shape=9;
        }
        if(shape==0){if(IsRail(get(p+new Vector3Int(0,1,-1))))shape=4;else if(IsRail(get(p+new Vector3Int(0,1,1))))shape=5;}
        if(shape==1){if(IsRail(get(p+new Vector3Int(1,1,0))))shape=2;else if(IsRail(get(p+new Vector3Int(-1,1,0))))shape=3;}
        return (byte)(shape|(t==BlockType.Rail?0:current&8));
    }
    private static bool RailAt(Vector3Int p,Vector3Int d,Func<Vector3Int,BlockType> get)=>IsRail(get(p+d))||IsRail(get(p+d+new Vector3Int(0,1,0)))||IsRail(get(p+d+new Vector3Int(0,-1,0)));
    public static bool TryUse(BlockType t,byte state,out byte next)
    {
        next=state;
        if(t==BlockType.WoodenDoor||t==BlockType.Trapdoor){next=(byte)(state^4);return true;}
        if(t==BlockType.Lever){next=(byte)(state^8);return true;}
        if(t==BlockType.RedstoneRepeater||t==BlockType.PoweredRepeater){next=(byte)((state+4)&15);return true;}
        return false;
    }
    public static Direction TextureDirection(BlockType t,byte state,Direction d)
    {
        if(d==Direction.up||d==Direction.down)return d;
        int q;
        if(t==BlockType.Furnace||t==BlockType.LitFurnace||t==BlockType.Dispenser||t==BlockType.Chest||t==BlockType.LockedChest)
            q=state==3?2:state==4?3:state==5?1:0;
        else if(t==BlockType.Pumpkin||t==BlockType.JackOLantern)q=(state+2)&3;
        else return d;
        int side=d==Direction.backwards?0:d==Direction.right?1:d==Direction.forwards?2:3;
        int local=(side-q+4)&3;
        return local==0?Direction.backwards:local==1?Direction.right:local==2?Direction.forwards:Direction.left;
    }
    public static byte Placement(BlockType t,Vector3 look,Vector3Int normal)
    {
        if(StairGeometry.IsStair(t))return StairGeometry.FacingForLook(look);
        byte q=Quarter(look);
        if(IsTorch(t)||t==BlockType.Lever||t==BlockType.StoneButton)
            return normal.x>0?(byte)1:normal.x<0?(byte)2:normal.z>0?(byte)3:normal.z<0?(byte)4:t==BlockType.Lever?(byte)((q&1)==0?5:6):(byte)5;
        if(t==BlockType.Ladder||t==BlockType.WallSign)
            return normal.z<0?(byte)2:normal.z>0?(byte)3:normal.x<0?(byte)4:(byte)5;
        if(t==BlockType.Trapdoor)return normal.z<0?(byte)0:normal.z>0?(byte)1:normal.x<0?(byte)2:(byte)3;
        if(t==BlockType.StandingSign)return (byte)((int)Math.Floor(Math.Atan2(-look.x,look.z)*8/Math.PI+8.5)&15);
        if(IsRail(t))return (byte)(q&1);
        if(t==BlockType.NetherPortal)return (byte)(q&1);
        if(t==BlockType.Furnace||t==BlockType.LitFurnace||t==BlockType.Dispenser||t==BlockType.Chest||t==BlockType.LockedChest)
            return q==0?(byte)2:q==1?(byte)5:q==2?(byte)3:(byte)4;
        if(t==BlockType.Piston||t==BlockType.StickyPiston||t==BlockType.PistonHead)
            return look.y>.7f?(byte)0:look.y<-.7f?(byte)1:q==0?(byte)2:q==1?(byte)5:q==2?(byte)3:(byte)4;
        if(IsDoor(t))return (byte)((q+1)&3);
        if(t==BlockType.Bed||t==BlockType.RedstoneRepeater||t==BlockType.PoweredRepeater||t==BlockType.Pumpkin||t==BlockType.JackOLantern)return q;
        return DefaultState(t);
    }
}
