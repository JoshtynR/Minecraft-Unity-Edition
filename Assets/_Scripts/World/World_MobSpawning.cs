using System.Collections.Generic;
using UnityEngine;

// Beta 1.7.3 SpawnerAnimals rules with the 17x17 eligible area cached until a
// player crosses a chunk boundary. The spawn attempts and random walks remain
// source-shaped; only repeated set construction and unloaded-chunk queries are
// removed from the hot path.
public partial class World
{
    [System.NonSerialized] private HashSet<Vector3Int> runtimeMobEligibleSet;
    private HashSet<Vector3Int> mobEligibleSet=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobEligibleSet,()=>new HashSet<Vector3Int>());
    [System.NonSerialized] private List<Vector3Int> runtimeMobPlayerChunks;
    private List<Vector3Int> mobPlayerChunks=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobPlayerChunks,()=>new List<Vector3Int>());
    private bool mobSpawnPointKnown;
    private Vector3 mobSpawnPoint;

    private void ResetMobSpawnCache()
    {
        if(runtimeMobEligibleSet!=null)runtimeMobEligibleSet.Clear();
        if(runtimeMobPlayerChunks!=null)runtimeMobPlayerChunks.Clear();
        mobSpawnPointKnown=false;mobSpawnPoint=Vector3.zero;
    }

    private void RefreshEligibleMobChunks()
    {
        bool changed=mobPlayerChunks.Count!=mobPlayers.Count;
        for(int i=0;i<mobPlayers.Count;i++)
        {
            var chunk=WorldDataHelper.GetChunkPosition(this,Vector3Int.FloorToInt(mobPlayers[i].transform.position));
            if(!changed&&mobPlayerChunks[i]!=chunk)changed=true;
        }
        if(!changed)return;
        mobPlayerChunks.Clear();mobEligibleSet.Clear();mobSpawnCandidates.Clear();
        for(int i=0;i<mobPlayers.Count;i++)
        {
            var center=WorldDataHelper.GetChunkPosition(this,Vector3Int.FloorToInt(mobPlayers[i].transform.position));
            mobPlayerChunks.Add(center);
            for(int x=-8;x<=8;x++)for(int z=-8;z<=8;z++)
                mobEligibleSet.Add(new Vector3Int(center.x+x*chunkSize,0,center.z+z*chunkSize));
        }
        foreach(var chunk in mobEligibleSet)mobSpawnCandidates.Add(chunk);
    }

    private Vector3 MobWorldSpawn()
    {
        if(mobSpawnPointKnown)return mobSpawnPoint;
        mobSpawnPointKnown=true;mobSpawnPoint=new Vector3(.5f,Mathf.Min(65,worldHeight-3)+1,.5f);
        return mobSpawnPoint;
    }

    internal void RememberMobWorldSpawn(Vector3 position)
    {mobSpawnPoint=position;mobSpawnPointKnown=true;}

    private void TrackMobCandidate(MobState mob)
    {
        if(mob!=null&&!mobTickCandidates.Contains(mob))mobTickCandidates.Add(mob);
        // Make a newly spawned mob visible immediately instead of waiting for
        // the quarter-second render-candidate refresh.
        if(mob!=null&&!mobVisualCandidates.Contains(mob))mobVisualCandidates.Add(mob);
    }

    private bool BetaSpawnCell(Vector3Int cell)
    {
        if(cell.y<1||cell.y+1>=worldHeight)return false;
        var below=GetBlock(cell+new Vector3Int(0,-1,0));var feet=GetBlock(cell);var head=GetBlock(cell+new Vector3Int(0,1,0));
        if(below==null||feet==null||head==null||below.type==BlockType.Nothing||feet.type==BlockType.Nothing||head.type==BlockType.Nothing)return false;
        if(!SpecialBlockGeometry.NormalCube(below.type)||SpecialBlockGeometry.NormalCube(feet.type)||SpecialBlockGeometry.NormalCube(head.type))return false;
        return feet.type!=BlockType.Water&&feet.type!=BlockType.Lava;
    }

    private bool BetaSpecificSpawn(MobType species,Vector3 feet)
    {
        var cell=Vector3Int.FloorToInt(feet);var light=GetBlock(cell);
        if(light==null)return false;
        if(MobRules.Hostile(species))
        {
            // EntityMob returns before consuming the second roll when raw
            // saved skylight fails. Daylight reduction only applies afterward.
            if(light.GetSkyLight()>mobRandom.NextInt(32))return false;
            if(!HostileSpawnRules.Allows(light.GetBlockLight(),light.GetSkyLight(),skyLightReduction,mobRandom.NextInt(8)))return false;
        }
        else
        {
            var floor=GetBlock(cell+new Vector3Int(0,-1,0));
            if(floor==null||floor.type!=BlockType.Grass||!HostileSpawnRules.PassiveAllows(light.GetBlockLight(),light.GetSkyLight()))return false;
        }
        return MobSpawnClear(feet,species);
    }

    private int BetaSheepColor()
    {
        int roll=mobRandom.NextInt(100);
        if(roll<5)return 15;
        if(roll<10)return 7;
        if(roll<15)return 8;
        if(roll<18)return 12;
        return mobRandom.NextInt(500)==0?6:0;
    }

    private void SpawnCreatureCategory(bool hostile)
    {
        if(dimension!=WorldDimension.Overworld||mobPlayers.Count==0||worldData.chunkDict==null||mobs.Count>=MobStore.Capacity)return;
        if(hostile?!spawnHostileMobs:!spawnPassiveMobs)return;
        RefreshEligibleMobChunks();int eligible=mobSpawnCandidates.Count;if(eligible==0)return;
        // Persisted mobs in unloaded chunks must not consume the active-area
        // cap. Beta counts entities in the eligible loaded region only.
        int cap=HostileSpawnRules.CategoryCap(hostile?maximumActiveHostiles:maximumActivePassiveMobs,eligible);
        int count=0;
        foreach(var entity in mobs.Entities)
            if(MobRules.Hostile(entity.species)==hostile&&MobAreaLoaded(entity.position)&&++count>cap)return;
        // SpawnerAnimals uses <=, so a final pack may carry the category over
        // the scaled cap before the next tick stops spawning it.
        if(count>cap)return;
        var worldSpawn=MobWorldSpawn();int start=mobRandom.NextInt(eligible);
        for(int index=0;index<eligible&&mobs.Count<MobStore.Capacity;index++)
        {
            var chunk=mobSpawnCandidates[(start+index)%eligible];
            if(!worldData.chunkDict.ContainsKey(chunk))continue;
            int speciesRoll=mobRandom.NextInt(hostile?50:40);
            // Slimes occupy weight 40..49 in Beta's monster list. They do not
            // have a registered model/brain yet, so preserve the failed slot
            // instead of inflating the other four monsters' spawn rates.
            if(hostile&&speciesRoll>=40)continue;
            var species=MobDefinitions.SelectSpawn(hostile,speciesRoll);
            int x=chunk.x+mobRandom.NextInt(16),y=mobRandom.NextInt(worldHeight),z=chunk.z+mobRandom.NextInt(16);
            var anchor=GetBlock(new Vector3Int(x,y,z));
            if(anchor==null||SpecialBlockGeometry.NormalCube(anchor.type)||anchor.type!=BlockType.Air)continue;
            int spawnedInChunk=0;
            for(int group=0;group<3&&spawnedInChunk<4;group++)
            {
                int px=x,py=y,pz=z;
                for(int attempt=0;attempt<4&&spawnedInChunk<4;attempt++)
                {
                    px+=mobRandom.NextInt(6)-mobRandom.NextInt(6);
                    // These calls both return zero, but still advance Java's RNG.
                    py+=mobRandom.NextInt(1)-mobRandom.NextInt(1);
                    pz+=mobRandom.NextInt(6)-mobRandom.NextInt(6);
                    var cell=new Vector3Int(px,py,pz);if(!BetaSpawnCell(cell))continue;
                    var feet=new Vector3(px+.5f,py,pz+.5f);
                    if(MobPlayerDistance(feet)<24*24||(feet-worldSpawn).sqrMagnitude<24*24)continue;
                    float yaw=mobRandom.NextFloat()*360;
                    if(!BetaSpecificSpawn(species,feet))continue;
                    var mob=mobs.Spawn(feet,yaw,species);if(mob==null)return;
                    TrackMobCandidate(mob);
                    if(species==MobType.Sheep)mob.fleeceColor=BetaSheepColor();
                    BroadcastMob(mob);spawnedInChunk++;
                }
            }
        }
    }
}
