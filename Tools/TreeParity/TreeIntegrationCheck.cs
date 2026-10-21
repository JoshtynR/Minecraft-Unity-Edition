using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x,y,z;
        public Vector3Int(int x,int y,int z) { this.x=x;this.y=y;this.z=z; }
        public bool Equals(Vector3Int p) { return x==p.x&&y==p.y&&z==p.z; }
        public override bool Equals(object p) { return p is Vector3Int && Equals((Vector3Int)p); }
        public override int GetHashCode() { unchecked{return(x*397^y)*397^z;} }
    }
}
public class Block { public ChunkData chunkData;public BlockType type; public Block(BlockType type){this.type=type;} }
public class World
{
    public ChunkData recordOwner;
    public HashSet<UnityEngine.Vector3Int> unavailable=new HashSet<UnityEngine.Vector3Int>();
    public Dictionary<UnityEngine.Vector3Int,BlockType> writes=new Dictionary<UnityEngine.Vector3Int,BlockType>();
    public void NotifyTreeChunkChanged(UnityEngine.Vector3Int p) {}
    public Block GetBlock(UnityEngine.Vector3Int p)
    {
        var origin=new UnityEngine.Vector3Int((int)Math.Floor(p.x/16.0)*16,0,(int)Math.Floor(p.z/16.0)*16);
        if(unavailable.Contains(origin))return new Block(BlockType.Nothing);
        BlockType type;
        if(writes.TryGetValue(p,out type))return new Block(type){chunkData=recordOwner};
        return new Block(p.y==63?BlockType.Grass:p.y<63?BlockType.Stone:BlockType.Air){chunkData=recordOwner};
    }
}
public class ChunkData { public World worldRef; public UnityEngine.Vector3Int worldPos; public bool betaPopulationDone,modifiedAfterSave;public List<BetaPopulationRecord> betaPopulationRecords=new List<BetaPopulationRecord>(); public ChunkSection[] sections=new ChunkSection[0]; }
public class ChunkSection {}
public class ChunkSectionSaveData { public ChunkSectionSaveData(ChunkSection s) {} }
public static class WorldDataHelper
{
    public static void SetBlock(World world,UnityEngine.Vector3Int p,BlockType type) { world.writes[p]=type; }
}
public class TreeIntegrationCheck
{
    private class CanonicalWorld : IBetaTreeWorld
    {
        public World storage=new World();
        public BlockType Get(int x,int y,int z) { return y<0||y>=128?BlockType.Air:storage.GetBlock(new UnityEngine.Vector3Int(x,y,z)).type; }
        public void Set(int x,int y,int z,BlockType type) { if(y>=0&&y<128)storage.writes[new UnityEngine.Vector3Int(x,y,z)]=type; }
        public bool IsOpaque(BlockType b){return b!=BlockType.Air&&b!=BlockType.Water&&b!=BlockType.Lava&&b!=BlockType.Glass&&!Beta173Trees.IsLeaves(b);}
    }
    public static int Main()
    {
        int checks=0;
        var type=typeof(Beta173Population).GetNestedType("TreeWorld",BindingFlags.NonPublic);
        for(int kind=0;kind<5;kind++)for(int i=0;i<16;i++)
        {
            var world=new World();var owner=new ChunkData{worldRef=world};
            var adapter=(IBetaTreeWorld)Activator.CreateInstance(type,owner);
            var canonical=new CanonicalWorld();
            Beta173Trees.Generate(canonical,new JavaRandom(i),(BetaTreeKind)kind,16,64,-17);
            Beta173Trees.Generate(adapter,new JavaRandom(i),(BetaTreeKind)kind,16,64,-17);
            if(canonical.storage.writes.Count!=world.writes.Count)throw new Exception("Tree adapter count mismatch");
            foreach(var pair in canonical.storage.writes)
            {
                var p=pair.Key;var reflected=new UnityEngine.Vector3Int(-p.x-1,p.y,p.z);
                if(!world.writes.ContainsKey(reflected)||world.writes[reflected]!=pair.Value)throw new Exception("Tree adapter reflection/material mismatch");
            }
            checks++;
        }
        // Height map must include pre-existing logs, leaves, water, and ice;
        // the old preview deliberately ignored logs/leaves and could overlap trees.
        foreach(var block in new[]{BlockType.Log,BlockType.Leaves,BlockType.SpruceLeaves,BlockType.BirchLog,BlockType.Water,BlockType.Ice})
        {
            var world=new World();world.writes[new UnityEngine.Vector3Int(-1,71,0)]=block;
            var adapter=Activator.CreateInstance(type,new ChunkData{worldRef=world});
            if((int)type.GetMethod("Height").Invoke(adapter,new object[]{0,0})!=72)throw new Exception("Tree-stage height map excluded "+block);
            checks++;
        }
        var edge=new World();var edgeOwner=new ChunkData{worldRef=edge};
        if(!Beta173Population.HasCompleteNeighborhood(edgeOwner,0,0))throw new Exception("available tree neighborhood rejected");checks++;
        edge.unavailable.Add(new UnityEngine.Vector3Int(-32,0,0));
        if(Beta173Population.HasCompleteNeighborhood(edgeOwner,0,0))throw new Exception("missing canopy neighbor accepted");checks++;
        edge.unavailable.Clear();
        if(!Beta173Population.HasCompleteNeighborhood(edgeOwner,0,0))throw new Exception("tree neighborhood did not resume");checks++;
        edgeOwner.modifiedAfterSave=true;
        var saved=ChunkSaveData.Serialize(edgeOwner);
        if(!edgeOwner.modifiedAfterSave)throw new Exception("Creating a network/save snapshot cleared unsaved dirty terrain");checks++;
        if(saved.betaTreePopulationVersion!=1 || saved.betaTreePopulationDone)throw new Exception("deferred tree state not saved");checks++;
        edgeOwner.betaPopulationDone=true;saved=ChunkSaveData.Serialize(edgeOwner);
        if(!saved.betaTreePopulationDone)throw new Exception("completed tree state not saved");checks++;
        edgeOwner.betaPopulationRecords.Add(new BetaPopulationRecord{kind="spawner",mob="Zombie",position=new UnityEngine.Vector3Int(-1,40,0)});
        saved=ChunkSaveData.Serialize(edgeOwner);
        if(saved.betaPopulationVersion!=1||!saved.betaPopulationDone||saved.betaPopulationRecords.Length!=1||saved.betaPopulationRecords[0].mob!="Zombie")throw new Exception("population completion/dungeon records not saved");checks++;
        var recordStorage=new World();var recordOwner=new ChunkData{worldRef=recordStorage};recordStorage.recordOwner=recordOwner;
        var population=(IBetaPopulationWorld)Activator.CreateInstance(type,recordOwner);
        population.Set(0,40,0,BlockType.Chest);population.Loot(0,40,0,1,265,3,0);
        var oldRecord=recordOwner.betaPopulationRecords[0];population.Loot(0,40,0,1,297,1,0);
        population.Spawner(1,40,0,"Zombie");population.Metadata(2,64,0,3);
        if(recordOwner.betaPopulationRecords.Count!=3||recordOwner.betaPopulationRecords[0].item!=297||oldRecord.item!=265)throw new Exception("loot slot overwrite/save snapshot ownership mismatch");checks++;
        if(recordOwner.betaPopulationRecords[0].position.x!=-1||recordOwner.betaPopulationRecords[1].position.x!=-2)throw new Exception("dungeon record coordinate reflection mismatch");checks++;
        Console.WriteLine(checks+" production tree adapter/reflection/height checks passed (Unity storage mocked).");return 0;
    }
}
