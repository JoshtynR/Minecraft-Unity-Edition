using System;
using System.Collections.Generic;
using UnityEngine;
public enum WorldDimension {Overworld,Nether}
public class Block {public BlockType type;public byte blockShape;}
public class ChunkRenderer {}
public class World
{
    public int worldHeight=128;
    public WorldDimension dimension;
    public Func<Vector3Int,BlockType> terrain=p=>BlockType.Air;
    public readonly Dictionary<Vector3Int,BlockType> edits=new Dictionary<Vector3Int,BlockType>();
    public Block GetBlock(Vector3Int p)=>new Block{type=edits.TryGetValue(p,out var type)?type:terrain(p)};
    public void RememberMobWorldSpawn(Vector3 p){}
    public void AddChunkToUpdate(ChunkRenderer c,bool first){}
}
public static class WorldDataHelper
{
    public static void SetBlock(World w,Vector3Int p,BlockType type){w.edits[p]=type;}
    public static Vector3Int GetChunkPosition(World w,Vector3Int p)=>Vector3Int.zero;
    public static ChunkRenderer GetChunk(World w,Vector3Int p)=>null;
}
public static class SpecialBlockGeometry
{
    public static bool NormalCube(BlockType type)=>type==BlockType.Stone||type==BlockType.Grass;
    public static byte State(BlockType type,byte state)=>state;
    public static Vector3Int PartnerOffset(BlockType type,byte state)=>new Vector3Int(1,0,0);
}
public static class SpawnCheck
{
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static void OpenSky(World world,Vector3 position)
    {
        var feet=Vector3Int.FloorToInt(position);
        Check(world.GetBlock(feet+Vector3Int.down).type==BlockType.Stone||world.GetBlock(feet+Vector3Int.down).type==BlockType.Grass,"solid floor");
        for(int y=feet.y;y<world.worldHeight;y++)
            Check(world.GetBlock(new Vector3Int(feet.x,y,feet.z)).type==BlockType.Air,"every cell to build limit must be air");
    }
    public static void Main()
    {
        var world=new World{terrain=p=>p.y<=10||p.y==70?BlockType.Stone:BlockType.Air};
        Check(WorldSpawnRules.TryFindNatural(world,out var spawn)&&spawn.y==71,"choose roof surface, not cave floor");OpenSky(world,spawn);
        world=new World{terrain=p=>p.y==64?BlockType.Grass:p.x==0&&p.z==0&&p.y==90?BlockType.Leaves:BlockType.Air};
        Check(WorldSpawnRules.TryFindNatural(world,out spawn)&&(spawn.x!=.5f||spawn.z!=.5f),"skip dirt/grass under canopy");OpenSky(world,spawn);
        world=new World{terrain=p=>p.y==64?BlockType.Water:p.y<60?BlockType.Stone:BlockType.Air};
        Check(!WorldSpawnRules.TryFindNatural(world,out spawn),"never descend through water to underwater floor");
        spawn=WorldSpawnRules.Resolve(world);OpenSky(world,spawn);
        world=new World{terrain=p=>p.y==120?BlockType.Leaves:p.y==10?BlockType.Stone:BlockType.Air};
        spawn=WorldSpawnRules.Resolve(world);Check(spawn.y==121,"fallback rises above obstructed columns");OpenSky(world,spawn);
        world=new World{terrain=p=>p.y==127?BlockType.Glass:p.y==10?BlockType.Stone:BlockType.Air};
        Check(!WorldSpawnRules.TryFindNatural(world,out spawn),"reject roof at build limit");
        spawn=WorldSpawnRules.Resolve(world);Check(spawn.y==126,"fallback preserves two body cells below build limit");OpenSky(world,spawn);
        world=new World{terrain=p=>p.y==100?BlockType.Nothing:p.y==64?BlockType.Stone:BlockType.Air};
        Check(!WorldSpawnRules.TryFindNatural(world,out spawn),"unknown cells are not open sky");
        world=new World{terrain=p=>p.x==15&&p.z==0&&p.y==64?BlockType.Stone:BlockType.Air};
        Check(WorldSpawnRules.TryFindNatural(world,out spawn)&&spawn.x==15.5f,"retain 15-block spawn search");
        world=new World{terrain=p=>p.x==16&&p.z==0&&p.y==64?BlockType.Stone:BlockType.Air};
        Check(!WorldSpawnRules.TryFindNatural(world,out spawn),"do not exceed search radius");
        // Beds intentionally remain valid indoors; this change targets natural world spawn.
        world=new World();world.edits[new Vector3Int(0,65,0)]=BlockType.Bed;world.edits[new Vector3Int(1,65,0)]=BlockType.Bed;
        world.edits[new Vector3Int(-1,64,0)]=BlockType.Stone;world.edits[new Vector3Int(-1,70,0)]=BlockType.Stone;
        Check(WorldSpawnRules.TryResolveBed(world,new Vector3Int(0,65,0),out spawn),"indoor bed respawn remains valid");
        Console.WriteLine(checks+" production open-sky spawn checks passed (world storage mocked).");
    }
}
