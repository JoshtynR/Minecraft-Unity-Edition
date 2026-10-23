using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public struct MobStateMessage : NetworkMessage { public WorldDimension dimension;public MobState mob; public bool removed; }
public struct MobBatchMessage : NetworkMessage { public WorldDimension dimension;public MobState[] mobs; public bool reset; }
public struct MobSyncRequest : NetworkMessage { }

public partial class World
{
    [Header("Mobs")]
    [UnityEngine.Serialization.FormerlySerializedAs("spawnCows")]
    [Tooltip("Enable natural cow, pig, sheep and chicken herds.")]
    public bool spawnPassiveMobs = true;
    [UnityEngine.Serialization.FormerlySerializedAs("maximumActiveCows")]
    [Range(0,64)] public int maximumActivePassiveMobs = 24;
    // sceneLoaded can restore saves before Start. Keep runtime stores outside
    // Unity serialization and initialize on access, including after script reload.
    [System.NonSerialized] private MobEntitySystem runtimeMobEntities;
    private MobEntitySystem mobEntities => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobEntities, () => new MobEntitySystem());
    public MobEntitySystem MobEntities=>mobEntities;
    public MobStore mobs=>mobEntities.Store;
    [System.NonSerialized] private Dictionary<Vector3Int,int> runtimeMobSpawnCooldowns;
    private Dictionary<Vector3Int,int> mobSpawnCooldowns => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobSpawnCooldowns, () => new Dictionary<Vector3Int,int>());
    [System.NonSerialized] private Dictionary<int,MobVisual> runtimeMobVisuals;
    private Dictionary<int,MobVisual> mobVisuals => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobVisuals, () => new Dictionary<int,MobVisual>());
    [System.NonSerialized] private List<MobState> runtimeActiveMobs;
    private List<MobState> activeMobs => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeActiveMobs, () => new List<MobState>());
    [System.NonSerialized] private List<Player> runtimeMobPlayers;
    private List<Player> mobPlayers => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobPlayers, () => new List<Player>());
    [System.NonSerialized] private List<Vector3Int> runtimeMobSpawnCandidates;
    private List<Vector3Int> mobSpawnCandidates => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobSpawnCandidates, () => new List<Vector3Int>());
    [System.NonSerialized] private List<int> runtimeMobVisualRemovals;
    private List<int> mobVisualRemovals => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobVisualRemovals, () => new List<int>());
    private MobSaveData mobRestore;
    private JavaRandom mobRandom;
    [System.NonSerialized] private Dictionary<MobType,Mesh[]> runtimeMobMeshesByType;
    private Dictionary<MobType,Mesh[]> mobMeshesByType => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobMeshesByType, () => new Dictionary<MobType,Mesh[]>());
    [System.NonSerialized] private Dictionary<MobType,Material> runtimeMobMaterialsByType;
    private Dictionary<MobType,Material> mobMaterialsByType => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeMobMaterialsByType, () => new Dictionary<MobType,Material>());
    private float mobSpawnClock;
    private int mobTick;
    private bool mobSyncRequested;

    private void InitializeMobs()
    {
        NetworkClient.RegisterHandler<MobArrowMessage>(message=>
        {var realm=FindDimension(message.dimension);if(realm!=null)realm.ReceiveMobArrow(message);});
        NetworkClient.RegisterHandler<MobStateMessage>(message =>
        {
            if (DropAuthority || message.mob == null) return;
            var world=FindDimension(message.dimension);if(world==null)return;
            if (message.removed) world.mobs.Remove(message.mob.id); else world.mobs.Apply(message.mob);
        });
        NetworkClient.RegisterHandler<MobBatchMessage>(message =>
        {
            if (DropAuthority) return;
            var world=FindDimension(message.dimension);if(world==null)return;
            if (message.reset) world.mobs.Restore(null);
            if (message.mobs != null) foreach (var mob in message.mobs) world.mobs.Apply(mob);
        });
    }
    private void EnsureMobsRestored()
    {
        if (mobRandom == null) mobRandom = new JavaRandom(unchecked(betaWorldSeed ^ 0x434F5753L));
        if (mobRestore == null) return;
        mobs.Restore(mobRestore);
        mobSpawnCooldowns.Clear();
        if (mobRestore.hasRandomState) mobRandom.RestoreState(mobRestore.randomState);
        mobRestore = null;
    }
    public MobSaveData CaptureMobs()
    {
        EnsureMobsRestored();
        return new MobSaveData { entities = mobs.Capture(), nextId = mobs.NextId, randomState = mobRandom.State, hasRandomState = true };
    }
    public void RestoreMobs(MobSaveData saved)
    {
        ResetMobs();
        mobRestore = DropAuthority ? saved : null;
    }
    public void SendMobSnapshot(NetworkConnectionToClient connection)
    {
        if (connection == null || connection.identity == null || !IsWorldCreated) return;
        EnsureMobsRestored();
        var batch = new List<MobState>(32); bool first = true;
        foreach (var mob in mobs.Entities)
        {
            batch.Add(mob.Copy());
            if (batch.Count != 32) continue;
            connection.Send(new MobBatchMessage { dimension=dimension,mobs = batch.ToArray(), reset = first }); first = false; batch.Clear();
        }
        if (batch.Count > 0 || first) connection.Send(new MobBatchMessage { dimension=dimension,mobs = batch.ToArray(), reset = first });
        SendMobArrows(connection);
    }
    private void BroadcastMob(MobState mob, bool removed = false)
    {
        if(removed)mobEntities.RemoveRuntime(mob.id);
        if (NetworkServer.active) NetworkServer.SendToAll(new MobStateMessage { dimension=dimension,mob = mob.Copy(), removed = removed });
    }
    private void RefreshMobPlayers()
    {
        mobPlayers.Clear();
        if (NetworkServer.active)
        {
            foreach (var connection in NetworkServer.connections.Values)
                if (connection != null && connection.identity != null)
                {
                    var player = connection.identity.GetComponent<Player>();
                    if (player != null && player.dimension == dimension && player.health > 0) mobPlayers.Add(player);
                }
        }
        else if (GameManager.Instance != null && GameManager.Instance.localPlayer != null && GameManager.Instance.localPlayer.health > 0 && GameManager.Instance.localPlayer.dimension == dimension)
            mobPlayers.Add(GameManager.Instance.localPlayer);
    }
    private bool MobAreaLoaded(Vector3 position)
    {
        if (worldData.chunkDict == null) return false;
        for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
        {
            var p = Vector3Int.FloorToInt(position + new Vector3(x * .5f, 0, z * .5f));
            if (!worldData.chunkDict.ContainsKey(WorldDataHelper.GetChunkPosition(this,p))) return false;
        }
        return true;
    }
    private float MobPlayerDistance(Vector3 position)
    {
        float distance = float.MaxValue;
        foreach (var player in mobPlayers)
        {
            var p = player.transform.position;
            distance = Mathf.Min(distance, (p-position).sqrMagnitude);
        }
        return distance;
    }
    private bool MobSpawnClear(Vector3 position,MobType species)
    {
        var candidate=new MobState{species=species,position=position};
        if(!MobWalkable(candidate,position))return false;
        foreach(var other in mobs.Entities)if(MobRules.Bounds(other).Intersects(MobRules.Bounds(candidate)))return false;
        return true;
    }
    private bool FindMobGround(int x,int z,bool grass,MobType species,out Vector3 feet)
    {
        feet = Vector3.zero;
        for (int y = worldHeight-2; y > 0; y--)
        {
            var block = GetBlock(new Vector3Int(x,y,z));
            if (block == null || block.type == BlockType.Nothing) return false;
            if (block.type == BlockType.Air || BlockShapesExtensions.IsPlant(block.type) || block.type == BlockType.SnowLayer) continue;
            if (grass && block.type != BlockType.Grass) return false;
            feet = new Vector3(x+.5f,y+1,z+.5f);
            if (!MobSpawnClear(feet,species)) return false;
            var light = GetBlock(Vector3Int.FloorToInt(feet));
            return !grass || Mathf.Max(light.GetBlockLight(),light.GetSkyLight()-skyLightReduction) > 8;
        }
        return false;
    }
    public bool SpawnMobNear(Vector3 origin)
    {return SpawnMobNear(origin,MobType.Cow);}
    public bool SpawnMobNear(Vector3 origin,MobType species)
    {
        if (!DropAuthority || !IsWorldCreated || isSaving) return false;
        EnsureMobsRestored();
        for (int i=0;i<16;i++)
        {
            int x = Mathf.FloorToInt(origin.x) + mobRandom.NextInt(7)-3;
            int z = Mathf.FloorToInt(origin.z) + mobRandom.NextInt(7)-3;
            if (!FindMobGround(x,z,false,species,out var feet)) continue;
            var mob = mobs.Spawn(feet,mobRandom.NextInt(360),species); if (mob == null) return false;
            BroadcastMob(mob); return true;
        }
        return false;
    }
    private void SpawnNaturalMobs()
    {
        int passiveCount=0;foreach(var animal in activeMobs)if(!MobRules.Hostile(animal.species))passiveCount++;
        if (!spawnPassiveMobs || skyLightReduction >= 7 || maximumActivePassiveMobs <= passiveCount || mobs.Count >= MobStore.Capacity || mobPlayers.Count == 0) return;
        mobSpawnCandidates.Clear();
        foreach(var entry in mobSpawnCooldowns)if(!worldData.chunkDict.ContainsKey(entry.Key)||mobTick>=entry.Value)mobSpawnCandidates.Add(entry.Key);
        foreach(var expired in mobSpawnCandidates)mobSpawnCooldowns.Remove(expired);mobSpawnCandidates.Clear();
        foreach (var p in worldData.chunkDict.Keys)
        {
            var center = (Vector3)p + new Vector3(chunkSize*.5f,0,chunkSize*.5f);
            float distance = MobPlayerDistance(center);
            if (distance < 72*72 && distance > 20*20 && !mobSpawnCooldowns.ContainsKey(p)) mobSpawnCandidates.Add(p);
        }
        // Visit only a bounded number of loaded chunks per second. This stream
        // never touches terrain/population RNG or rewrites terrain blocks.
        for (int visited=0;visited<8 && mobSpawnCandidates.Count>0;visited++)
        {
            int index=mobRandom.NextInt(mobSpawnCandidates.Count);var chunk=mobSpawnCandidates[index];mobSpawnCandidates.RemoveAt(index);
            mobSpawnCooldowns[chunk]=mobTick+200;
            if(mobRandom.NextInt(4)!=0)continue;
            int herd=2+mobRandom.NextInt(3),spawned=0;
            var species=(MobType)mobRandom.NextInt(4);
            for(int attempt=0;attempt<24 && spawned<herd && passiveCount+spawned<maximumActivePassiveMobs;attempt++)
            {
                int x=chunk.x+mobRandom.NextInt(chunkSize),z=chunk.z+mobRandom.NextInt(chunkSize);
                if(!FindMobGround(x,z,true,species,out var feet) || MobPlayerDistance(feet)<24*24)continue;
                var mob=mobs.Spawn(feet,mobRandom.NextInt(360),species);if(mob==null)break;
                BroadcastMob(mob);spawned++;
            }
            if(spawned>0)break;
        }
    }
    private bool DamageMob(MobState mob,int damage,Vector3 away)
    {
        if (!MobRules.Hurt(mob,damage,away)) return false;
        var definition=MobDefinitions.Get(mob.species);
        GameAudio.Broadcast(mob.health==0?definition.DeathSound:definition.HurtSound,mob.position,.5f,GameAudio.Variation(.9f,1.1f),this);
        if (mob.health == 0)
        {
            // Remove first: repeated attacks/packets cannot duplicate the drop.
            if (mobs.Remove(mob.id))
            {
                if(MobRules.Hostile(mob.species))DropHostileLoot(mob);
                else if(mob.ageTicks==0)
                {
                    bool cooked=mob.fireTicks>0;
                    if(mob.species==MobType.Cow)
                    {
                        int count=MobRules.LeatherCount(bound=>mobRandom.NextInt(bound));
                        if(count>0)SpawnItemDrop(new InventoryStack(ItemType.Leather,count),mob.position+Vector3.up*.5f);
                        SpawnItemDrop(new InventoryStack(cooked?ItemType.CookedBeef:ItemType.RawBeef,1+mobRandom.NextInt(3)),mob.position+Vector3.up*.5f);
                    }
                    else if(mob.species==MobType.Pig)SpawnItemDrop(new InventoryStack(cooked?ItemType.CookedPorkchop:ItemType.RawPorkchop,1+mobRandom.NextInt(3)),mob.position+Vector3.up*.5f);
                    else if(mob.species==MobType.Chicken)
                    {
                        SpawnItemDrop(new InventoryStack(cooked?ItemType.CookedChicken:ItemType.RawChicken,1),mob.position+Vector3.up*.5f);
                        int feathers=mobRandom.NextInt(3);if(feathers>0)SpawnItemDrop(new InventoryStack(ItemType.Feather,feathers),mob.position+Vector3.up*.5f);
                    }
                    else if(!mob.sheared)SpawnItemDrop(new InventoryStack(MobRules.Wool(mob),1),mob.position+Vector3.up*.5f);
                }
                BroadcastMob(mob,true);
            }
        }
        else BroadcastMob(mob);
        return true;
    }
    public bool HitMob(int id,int damage,Vector3 attacker)
    {
        if (!DropAuthority || isSaving) return false;
        EnsureMobsRestored();var mob=mobs.Get(id);
        return mob!=null && MobAreaLoaded(mob.position) && DamageMob(mob,damage,mob.position-attacker);
    }
    public bool InteractMob(int id,InventoryStack[] bag,int slot)
    {
        if(!DropAuthority||isSaving||bag==null||slot<0||slot>=bag.Length||bag[slot].Empty)return false;
        EnsureMobsRestored();
        var mob=mobs.Get(id);if(mob==null)return false;
        bool accepted=false;
        if(bag[slot].type==MobRules.BreedingFood(mob)&&MobRules.Feed(mob))
        {bag[slot].count--;bag[slot].Normalize();accepted=true;}
        else if(mob.species==MobType.Cow&&mob.ageTicks==0&&bag[slot].type==ItemType.Bucket)accepted=MobRules.Milk(bag,slot);
        else if(mob.species==MobType.Sheep&&mob.ageTicks==0&&!mob.sheared&&bag[slot].type==ItemType.Shears)
        {
            if(!SpawnItemDrop(new InventoryStack(MobRules.Wool(mob),1+mobRandom.NextInt(3)),mob.position))return false;
            mob.sheared=true;ItemDurability.Wear(ref bag[slot],1);accepted=true;
            GameAudio.Broadcast("mob/sheep/shear",mob.position,.5f,1,this);
        }
        else if(mob.species==MobType.Sheep&&MobRules.DyeColor(bag[slot].type)>=0&&mob.fleeceColor!=MobRules.DyeColor(bag[slot].type))
        {mob.fleeceColor=MobRules.DyeColor(bag[slot].type);bag[slot].count--;bag[slot].Normalize();accepted=true;}
        if(accepted)BroadcastMob(mob);return accepted;
    }
    public MobState TargetMob(Vector3 eye,Vector3 direction,float reach)
    {
        if (!MobRules.Finite(eye) || !MobRules.Finite(direction) || reach<=0 || direction.sqrMagnitude<.00001f) return null;
        direction.Normalize();
        float limit=reach;
        var block=BlockTargeting.Raycast(this,eye,direction,reach,out _,out var point);
        if(block!=null)limit=Mathf.Min(limit,(point-eye).magnitude);
        var ray=new Ray(eye,direction);MobState nearest=null;
        foreach(var mob in mobs.Entities)
            if(MobAreaLoaded(mob.position) && MobRules.Bounds(mob).IntersectRay(ray,out float distance) && distance>=0 && distance<limit)
            {nearest=mob;limit=distance;}
        return nearest;
    }
    public bool MobAccessible(int id,Vector3 eye,float reach)
    {
        var mob=mobs.Get(id);if(mob==null || !MobAreaLoaded(mob.position))return false;
        var destination=MobRules.Bounds(mob).ClosestPoint(eye);
        if((destination-eye).sqrMagnitude>reach*reach)return false;
        return TargetMob(eye,mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.5f)-eye,reach)==mob;
    }
    private void TickMob(MobState mob)
    {
        float width=MobRules.Width(mob),height=MobRules.BodyHeight(mob);
        if(mob.hurtTicks>0)mob.hurtTicks--;
        if(mob.hurtCooldown>0)mob.hurtCooldown--;
        if(mob.fleeTicks>0)mob.fleeTicks--;
        bool water=FluidWorldQueries.Touches(this,mob.position,width,height,BlockType.Water,out var current);
        bool lava=FluidWorldQueries.Touches(this,mob.position,width,height,BlockType.Lava,out _);
        bool fire=FluidWorldQueries.Touches(this,mob.position,width,height,BlockType.Fire,out _);
        mob.grounded=BlockCollision.Supported(this,mob.position,width,height);
        if(--mob.decisionTicks<=0)
        {
            mob.decisionTicks=40+mobRandom.NextInt(100);mob.walking=mobRandom.NextInt(3)!=0;
            if(mob.fleeTicks<=0)mob.targetYaw=mobRandom.NextInt(360);
        }
        if(!TickMobBrain(mob))return;
        mob.yaw=Mathf.MoveTowardsAngle(mob.yaw,mob.targetYaw,15);
        float speed=mob.walking?MobDefinitions.Get(mob.species).WalkSpeed:0;
        if(mob.fleeTicks>0)speed*=1.8f;
        float angle=mob.yaw*Mathf.Deg2Rad;
        var desired=new Vector3(Mathf.Sin(angle)*speed,0,Mathf.Cos(angle)*speed);
        mob.motion.x=Mathf.Lerp(mob.motion.x,desired.x,mob.grounded?.35f:.1f);
        mob.motion.z=Mathf.Lerp(mob.motion.z,desired.z,mob.grounded?.35f:.1f);
        if(water||lava)
        {
            mob.motion.y=Mathf.Min(.12f,mob.motion.y*.8f+.02f);
            mob.motion.x+=(float)current.x*.014f;mob.motion.z+=(float)current.z*.014f;mob.fallDistance=0;
        }
        else if(mob.grounded&&mob.motion.y<=0)mob.motion.y=-.08f;
        if(!MobAreaLoaded(mob.position+new Vector3(mob.motion.x,0,mob.motion.z)))return;
        var move=BlockCollision.Resolve(this,mob.position,width,height,mob.motion,mob.grounded);
        mob.position+=move;
        bool landed=mob.motion.y<0 && move.y>mob.motion.y+.0001f;
        if(!water && !lava && move.y<0)mob.fallDistance-=move.y;
        if(landed)
        {
            int damage=mob.species==MobType.Chicken?0:Mathf.Max(0,Mathf.CeilToInt(mob.fallDistance-3));mob.fallDistance=0;
            if(damage>0){DamageMob(mob,damage,Vector3.zero);if(mob.health<=0)return;}
        }
        if(Mathf.Abs(move.y-mob.motion.y)>.0001f)mob.motion.y=0;
        if(!water&&!lava&&!landed)
        {mob.motion.y=(mob.motion.y-.08f)*.98f;if(mob.species==MobType.Chicken&&mob.motion.y<0)mob.motion.y*=.6f;}
        bool blocked=Mathf.Abs(move.x-mob.motion.x)>.0001f||Mathf.Abs(move.z-mob.motion.z)>.0001f;
        if(blocked && (mob.grounded||landed))
        {mob.motion.y=.42f;mob.targetYaw+=mobRandom.NextInt(61)-30;}
        if(blocked&&mob.species==MobType.Spider)mob.motion.y=.2f;
        if(mob.position.y<-64){mobs.Remove(mob.id);BroadcastMob(mob,true);return;}
        if(FluidWorldQueries.AtEye(this,mob.position+Vector3.up*(height*.9f))==BlockType.Water)
        {
            if(--mob.air<=-20){mob.air=0;DamageMob(mob,2,Vector3.zero);}
        }
        else mob.air=300;
        if(mob.health<=0)return;
        if(water)mob.fireTicks=0;
        if(lava){mob.fireTicks=300;if(mobTick%10==0)DamageMob(mob,4,Vector3.zero);}
        else if(fire){mob.fireTicks=160;if(mobTick%20==0)DamageMob(mob,1,Vector3.zero);}
        else if(mob.fireTicks>0){mob.fireTicks--;if(mobTick%20==0)DamageMob(mob,1,Vector3.zero);}
    }
    private void UpdateMobs()
    {
        if(!Application.isPlaying || disabled || !IsWorldCreated || GenerationBusy)return;
        if(DropAuthority)
        {
            EnsureMobsRestored();
            if(!isSaving)
            {
                RefreshMobPlayers();mobEntities.Accumulate(Time.deltaTime);
                while(mobEntities.TryStep())
                {
                    mobTick=mobEntities.Tick;activeMobs.Clear();mobNavigationBudget=2;
                    foreach(var mob in mobs.Entities)
                    {
                        bool loaded=MobAreaLoaded(mob.position);float distance=MobPlayerDistance(mob.position);
                        if(!loaded||distance>=80*80)mobEntities.RemoveRuntime(mob.id);
                        if(loaded&&(distance<80*80||MobRules.Hostile(mob.species)))activeMobs.Add(mob);
                    }
                    int start=(int)((uint)mobTick%(uint)Math.Max(1,activeMobs.Count));
                    for(int index=0;index<activeMobs.Count;index++)
                    {
                        var mob=activeMobs[(start+index)%activeMobs.Count];
                        if(MobRules.Hostile(mob.species)&&MobPlayerDistance(mob.position)>128*128){mobs.Remove(mob.id);BroadcastMob(mob,true);continue;}
                        if(MobPlayerDistance(mob.position)>=80*80)continue;
                        TickMob(mob);if(mob.health>0&&mobs.Get(mob.id)!=null&&mobTick%3==0)BroadcastMob(mob);
                    }
                    mobEntities.Separate(activeMobs);
                    TickMobArrows();
                    mobSpawnClock+=.05f;
                    if(mobSpawnClock>=1){mobSpawnClock=0;SpawnNaturalMobs();SpawnNaturalHostiles();}
                }
            }
        }
        else if(!mobSyncRequested && NetworkClient.ready && NetworkClient.localPlayer!=null)
        {mobSyncRequested=true;NetworkClient.Send(new MobSyncRequest());}
        if(Instance==this&&(!NetworkServer.active || NetworkClient.active))UpdateMobVisuals();
    }
    private void UpdateMobVisuals()
    {
        mobVisualRemovals.Clear();
        foreach(var pair in mobVisuals)
        {
            var mob=mobs.Get(pair.Key);
            if(mob==null ? !pair.Value.AnimateDeath(Time.deltaTime) : !MobAreaLoaded(mob.position))mobVisualRemovals.Add(pair.Key);
        }
        foreach(int id in mobVisualRemovals){mobVisuals[id].Dispose();mobVisuals.Remove(id);}
        foreach(var mob in mobs.Entities)
        {
            bool visible=MobAreaLoaded(mob.position);
            if(!mobVisuals.TryGetValue(mob.id,out var visual) && visible)
            {
                if(!EnsureMobMeshes(mob.species))continue;
                visual=new MobVisual(worldRenderer.transform,mob,mobMeshesByType[mob.species],mobMaterialsByType[mob.species]);mobVisuals.Add(mob.id,visual);
            }
            if(visual!=null){visual.root.SetActive(visible);if(visible)visual.Update(mob,this,Time.deltaTime);}
        }
    }
    private bool EnsureMobMeshes(MobType species)
    {
        if(mobMaterialsByType.ContainsKey(species))return true;
        var template=Resources.Load<Material>(MobDefinitions.Get(species).MaterialResource);if(template==null||template.mainTexture==null)return false;
        var material=new Material(template);var meshes=new Mesh[MobModelGeometry.PartCountFor(species)];
        float height=64f*template.mainTexture.height/template.mainTexture.width;
        for(int i=0;i<meshes.Length;i++)
        {
            var data=MobModelGeometry.Build(i,height,species);var mesh=new Mesh{name=species+" part "+i};
            if(data.vertices.Count==0){Destroy(mesh);continue;}
            mesh.SetVertices(data.vertices);mesh.SetTriangles(data.triangles,0);
            var uv=new List<Vector2>();foreach(var p in data.uv)uv.Add(new Vector2(p.x,p.y));mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[i]=mesh;
        }
        mobMaterialsByType[species]=material;mobMeshesByType[species]=meshes;return true;
    }
    private void ResetMobs(bool discardSave=false)
    {
        ResetMobArrows();
        ResetMobEffects();
        foreach(var pair in mobMeshesByType)foreach(var mesh in pair.Value)if(mesh!=null)Destroy(mesh);
        foreach(var pair in mobMaterialsByType)if(pair.Value!=null)Destroy(pair.Value);
        mobMeshesByType.Clear();mobMaterialsByType.Clear();
        foreach(var visual in mobVisuals.Values)visual.Dispose();mobVisuals.Clear();
        mobEntities.Clear();mobSpawnCooldowns.Clear();activeMobs.Clear();mobPlayers.Clear();mobSpawnCandidates.Clear();mobVisualRemovals.Clear();
        mobSpawnClock=0;mobTick=0;mobSyncRequested=false;mobRandom=null;
        if(discardSave)mobRestore=null;
    }
}
