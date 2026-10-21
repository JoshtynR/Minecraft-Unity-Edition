using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public struct CowStateMessage : NetworkMessage { public WorldDimension dimension;public CowState cow; public bool removed; }
public struct CowBatchMessage : NetworkMessage { public WorldDimension dimension;public CowState[] cows; public bool reset; }
public struct CowSyncRequest : NetworkMessage { }

public partial class World
{
    [Header("Animals")]
    public bool spawnCows = true;
    [Range(0,64)] public int maximumActiveCows = 24;
    public readonly CowStore cows = new CowStore();
    private readonly HashSet<Vector3Int> cowPopulatedChunks = new HashSet<Vector3Int>();
    private readonly Dictionary<int,CowVisual> cowVisuals = new Dictionary<int,CowVisual>();
    private readonly List<CowState> activeCows = new List<CowState>();
    private readonly List<Player> cowPlayers = new List<Player>();
    private readonly List<Vector3Int> cowSpawnCandidates = new List<Vector3Int>();
    private readonly List<int> cowVisualRemovals = new List<int>();
    private CowSaveData cowRestore;
    private JavaRandom cowRandom;
    private Mesh[] cowMeshes;
    private Material cowMaterial;
    private float cowClock, cowSpawnClock;
    private int cowTick;
    private bool cowSyncRequested;

    private void InitializeCows()
    {
        NetworkClient.RegisterHandler<CowStateMessage>(message =>
        {
            if (DropAuthority || message.cow == null) return;
            var world=FindDimension(message.dimension);if(world==null)return;
            if (message.removed) world.cows.Remove(message.cow.id); else world.cows.Apply(message.cow);
        });
        NetworkClient.RegisterHandler<CowBatchMessage>(message =>
        {
            if (DropAuthority) return;
            var world=FindDimension(message.dimension);if(world==null)return;
            if (message.reset) world.cows.Restore(null);
            if (message.cows != null) foreach (var cow in message.cows) world.cows.Apply(cow);
        });
    }
    private void EnsureCowsRestored()
    {
        if (cowRandom == null) cowRandom = new JavaRandom(unchecked(betaWorldSeed ^ 0x434F5753L));
        if (cowRestore == null) return;
        cows.Restore(cowRestore);
        cowPopulatedChunks.Clear();
        if (cowRestore.populatedChunks != null)
            foreach (var p in cowRestore.populatedChunks) cowPopulatedChunks.Add(p);
        if (cowRestore.hasRandomState) cowRandom.RestoreState(cowRestore.randomState);
        cowRestore = null;
    }
    public CowSaveData CaptureCows()
    {
        EnsureCowsRestored();
        var chunks = new Vector3Int[cowPopulatedChunks.Count]; cowPopulatedChunks.CopyTo(chunks);
        return new CowSaveData { animals = cows.Capture(), nextId = cows.NextId, populatedChunks = chunks, randomState = cowRandom.State, hasRandomState = true };
    }
    public void RestoreCows(CowSaveData saved)
    {
        ResetCows();
        cowRestore = DropAuthority ? saved : null;
    }
    public void SendCowSnapshot(NetworkConnectionToClient connection)
    {
        if (connection == null || connection.identity == null || !IsWorldCreated) return;
        EnsureCowsRestored();
        var batch = new List<CowState>(32); bool first = true;
        foreach (var cow in cows.Animals)
        {
            batch.Add(cow.Copy());
            if (batch.Count != 32) continue;
            connection.Send(new CowBatchMessage { dimension=dimension,cows = batch.ToArray(), reset = first }); first = false; batch.Clear();
        }
        if (batch.Count > 0 || first) connection.Send(new CowBatchMessage { dimension=dimension,cows = batch.ToArray(), reset = first });
    }
    private void BroadcastCow(CowState cow, bool removed = false)
    {
        if (NetworkServer.active) NetworkServer.SendToAll(new CowStateMessage { dimension=dimension,cow = cow.Copy(), removed = removed });
    }
    private void RefreshCowPlayers()
    {
        cowPlayers.Clear();
        if (NetworkServer.active)
        {
            foreach (var connection in NetworkServer.connections.Values)
                if (connection != null && connection.identity != null)
                {
                    var player = connection.identity.GetComponent<Player>();
                    if (player != null && player.dimension == dimension && player.health > 0) cowPlayers.Add(player);
                }
        }
        else if (GameManager.Instance != null && GameManager.Instance.localPlayer != null && GameManager.Instance.localPlayer.health > 0 && GameManager.Instance.localPlayer.dimension == dimension)
            cowPlayers.Add(GameManager.Instance.localPlayer);
    }
    private bool CowAreaLoaded(Vector3 position)
    {
        if (worldData.chunkDict == null) return false;
        for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
        {
            var p = Vector3Int.FloorToInt(position + new Vector3(x * .5f, 0, z * .5f));
            if (!worldData.chunkDict.ContainsKey(WorldDataHelper.GetChunkPosition(this,p))) return false;
        }
        return true;
    }
    private float CowPlayerDistance(Vector3 position)
    {
        float distance = float.MaxValue;
        foreach (var player in cowPlayers)
        {
            var p = player.transform.position;
            float x = p.x - position.x, z = p.z - position.z;
            distance = Mathf.Min(distance, x*x + z*z);
        }
        return distance;
    }
    private bool CowSpawnClear(Vector3 position)
    {
        if (!CowAreaLoaded(position) || position.y < 1 || position.y + CowRules.Height >= worldHeight) return false;
        for (int x = Mathf.FloorToInt(position.x-CowRules.HalfWidth); x <= Mathf.FloorToInt(position.x+CowRules.HalfWidth); x++)
        for (int z = Mathf.FloorToInt(position.z-CowRules.HalfWidth); z <= Mathf.FloorToInt(position.z+CowRules.HalfWidth); z++)
        for (int y = Mathf.FloorToInt(position.y); y <= Mathf.FloorToInt(position.y+CowRules.Height); y++)
        {
            var cell = new Vector3Int(x,y,z); var block = GetBlock(cell);
            if (block == null || block.type == BlockType.Nothing || block.type == BlockType.Water || block.type == BlockType.Lava || block.type == BlockType.Fire) return false;
            if (!BlockShapesExtensions.IsPlant(block.type) && !SpecialBlockGeometry.NoCollision(block.type) && block.BlockData.generateCollider)
                for (int part=0; part<SpecialBlockGeometry.CollisionCount(block.type); part++)
                    if (BlockCollision.OverlapsBody(position,CowRules.HalfWidth,CowRules.Height,cell,SpecialBlockGeometry.CollisionPart(block.type,block.blockShape,part))) return false;
        }
        foreach (var cow in cows.Animals)
            if (CowRules.Bounds(cow).Intersects(new Bounds(position+Vector3.up*(CowRules.Height*.5f),new Vector3(.9f,1.3f,.9f)))) return false;
        return BlockCollision.Supported(this,position,CowRules.HalfWidth,CowRules.Height);
    }
    private bool FindCowGround(int x,int z,bool grass,out Vector3 feet)
    {
        feet = Vector3.zero;
        for (int y = worldHeight-2; y > 0; y--)
        {
            var block = GetBlock(new Vector3Int(x,y,z));
            if (block == null || block.type == BlockType.Nothing) return false;
            if (block.type == BlockType.Air || BlockShapesExtensions.IsPlant(block.type) || block.type == BlockType.SnowLayer) continue;
            if (grass && block.type != BlockType.Grass) return false;
            feet = new Vector3(x+.5f,y+1,z+.5f);
            if (!CowSpawnClear(feet)) return false;
            var light = GetBlock(Vector3Int.FloorToInt(feet));
            return !grass || Mathf.Max(light.GetBlockLight(),light.GetSkyLight()-betaSkylightSubtracted) > 8;
        }
        return false;
    }
    public bool SpawnCowNear(Vector3 origin)
    {
        if (!DropAuthority || !IsWorldCreated || isSaving) return false;
        EnsureCowsRestored();
        for (int i=0;i<16;i++)
        {
            int x = Mathf.FloorToInt(origin.x) + cowRandom.NextInt(7)-3;
            int z = Mathf.FloorToInt(origin.z) + cowRandom.NextInt(7)-3;
            if (!FindCowGround(x,z,false,out var feet)) continue;
            var cow = cows.Spawn(feet,cowRandom.NextInt(360)); if (cow == null) return false;
            BroadcastCow(cow); return true;
        }
        return false;
    }
    private void SpawnNaturalCows()
    {
        if (!spawnCows || betaSkylightSubtracted >= 7 || maximumActiveCows <= activeCows.Count || cows.Count >= CowStore.Capacity || cowPlayers.Count == 0) return;
        cowSpawnCandidates.Clear();
        foreach (var p in worldData.chunkDict.Keys)
        {
            var center = (Vector3)p + new Vector3(chunkSize*.5f,0,chunkSize*.5f);
            float distance = CowPlayerDistance(center);
            if (distance < 72*72 && distance > 20*20 && !cowPopulatedChunks.Contains(p)) cowSpawnCandidates.Add(p);
        }
        // Visit only a bounded number of loaded chunks per second. This stream
        // never touches terrain/population RNG or rewrites terrain blocks.
        for (int visited=0;visited<8 && cowSpawnCandidates.Count>0;visited++)
        {
            int index=cowRandom.NextInt(cowSpawnCandidates.Count);var chunk=cowSpawnCandidates[index];cowSpawnCandidates.RemoveAt(index);
            cowPopulatedChunks.Add(chunk);
            if(cowRandom.NextInt(4)!=0)continue;
            int herd=2+cowRandom.NextInt(3),spawned=0;
            for(int attempt=0;attempt<24 && spawned<herd && activeCows.Count+spawned<maximumActiveCows;attempt++)
            {
                int x=chunk.x+cowRandom.NextInt(chunkSize),z=chunk.z+cowRandom.NextInt(chunkSize);
                if(!FindCowGround(x,z,true,out var feet) || CowPlayerDistance(feet)<24*24)continue;
                var cow=cows.Spawn(feet,cowRandom.NextInt(360));if(cow==null)break;
                BroadcastCow(cow);spawned++;
            }
            if(spawned>0)break;
        }
    }
    private bool DamageCow(CowState cow,int damage,Vector3 away)
    {
        if (!CowRules.Hurt(cow,damage,away)) return false;
        GameAudio.Broadcast("mob/cow/hurt",cow.position,.5f,GameAudio.Variation(.9f,1.1f),this);
        if (cow.health == 0)
        {
            // Remove first: repeated attacks/packets cannot duplicate the drop.
            if (cows.Remove(cow.id))
            {
                int count=CowRules.LeatherCount(bound=>cowRandom.NextInt(bound));
                if(count>0)SpawnItemDrop(new InventoryStack(ItemType.Leather,count),cow.position+Vector3.up*.5f);
                BroadcastCow(cow,true);
            }
        }
        else BroadcastCow(cow);
        return true;
    }
    public bool HitCow(int id,int damage,Vector3 attacker)
    {
        if (!DropAuthority || isSaving) return false;
        EnsureCowsRestored();var cow=cows.Get(id);
        return cow!=null && CowAreaLoaded(cow.position) && DamageCow(cow,damage,cow.position-attacker);
    }
    public CowState TargetCow(Vector3 eye,Vector3 direction,float reach)
    {
        if (!CowRules.Finite(eye) || !CowRules.Finite(direction) || reach<=0 || direction.sqrMagnitude<.00001f) return null;
        direction.Normalize();
        float limit=reach;
        var block=BlockTargeting.Raycast(this,eye,direction,reach,out _,out var point);
        if(block!=null)limit=Mathf.Min(limit,(point-eye).magnitude);
        var ray=new Ray(eye,direction);CowState nearest=null;
        foreach(var cow in cows.Animals)
            if(CowAreaLoaded(cow.position) && CowRules.Bounds(cow).IntersectRay(ray,out float distance) && distance>=0 && distance<limit)
            {nearest=cow;limit=distance;}
        return nearest;
    }
    public bool CowAccessible(int id,Vector3 eye,float reach)
    {
        var cow=cows.Get(id);if(cow==null || !CowAreaLoaded(cow.position))return false;
        var destination=CowRules.Bounds(cow).ClosestPoint(eye);
        if((destination-eye).sqrMagnitude>reach*reach)return false;
        return TargetCow(eye,cow.position+Vector3.up*(CowRules.Height*.5f)-eye,reach)==cow;
    }
    private void TickCow(CowState cow)
    {
        if(cow.hurtTicks>0)cow.hurtTicks--;
        if(cow.hurtCooldown>0)cow.hurtCooldown--;
        if(cow.fleeTicks>0)cow.fleeTicks--;
        bool water=FluidWorldQueries.Touches(this,cow.position,CowRules.HalfWidth,CowRules.Height,BlockType.Water,out var current);
        bool lava=FluidWorldQueries.Touches(this,cow.position,CowRules.HalfWidth,CowRules.Height,BlockType.Lava,out _);
        bool fire=FluidWorldQueries.Touches(this,cow.position,CowRules.HalfWidth,CowRules.Height,BlockType.Fire,out _);
        cow.grounded=BlockCollision.Supported(this,cow.position,CowRules.HalfWidth,CowRules.Height);
        if(--cow.decisionTicks<=0)
        {
            cow.decisionTicks=40+cowRandom.NextInt(100);cow.walking=cowRandom.NextInt(3)!=0;
            if(cow.fleeTicks<=0)cow.targetYaw=cowRandom.NextInt(360);
        }
        cow.yaw=Mathf.MoveTowardsAngle(cow.yaw,cow.targetYaw,15);
        float speed=cow.fleeTicks>0?.12f:cow.walking?.065f:0;
        float angle=cow.yaw*Mathf.Deg2Rad;
        var desired=new Vector3(Mathf.Sin(angle)*speed,0,Mathf.Cos(angle)*speed);
        cow.motion.x=Mathf.Lerp(cow.motion.x,desired.x,cow.grounded?.35f:.1f);
        cow.motion.z=Mathf.Lerp(cow.motion.z,desired.z,cow.grounded?.35f:.1f);
        if(water||lava)
        {
            cow.motion.y=Mathf.Min(.12f,cow.motion.y*.8f+.02f);
            cow.motion.x+=(float)current.x*.014f;cow.motion.z+=(float)current.z*.014f;cow.fallDistance=0;
        }
        else cow.motion.y=(cow.motion.y-.08f)*.98f;
        if(!CowAreaLoaded(cow.position+new Vector3(cow.motion.x,0,cow.motion.z)))return;
        var move=BlockCollision.Resolve(this,cow.position,CowRules.HalfWidth,CowRules.Height,cow.motion,cow.grounded);
        cow.position+=move;
        bool landed=cow.motion.y<0 && move.y>cow.motion.y+.0001f;
        if(!water && !lava && move.y<0)cow.fallDistance-=move.y;
        if(landed)
        {
            int damage=Mathf.Max(0,Mathf.CeilToInt(cow.fallDistance-3));cow.fallDistance=0;
            if(damage>0){DamageCow(cow,damage,Vector3.zero);if(cow.health<=0)return;}
        }
        if(Mathf.Abs(move.y-cow.motion.y)>.0001f)cow.motion.y=0;
        bool blocked=Mathf.Abs(move.x-cow.motion.x)>.0001f||Mathf.Abs(move.z-cow.motion.z)>.0001f;
        if(blocked && (cow.grounded||landed))
        {cow.motion.y=.42f;cow.targetYaw+=cowRandom.NextInt(61)-30;}
        if(cow.position.y<-64){cows.Remove(cow.id);BroadcastCow(cow,true);return;}
        if(FluidWorldQueries.AtEye(this,cow.position+Vector3.up*1.2f)==BlockType.Water)
        {
            if(--cow.air<=-20){cow.air=0;DamageCow(cow,2,Vector3.zero);}
        }
        else cow.air=300;
        if(cow.health<=0)return;
        if(water)cow.fireTicks=0;
        if(lava){cow.fireTicks=300;if(cowTick%10==0)DamageCow(cow,4,Vector3.zero);}
        else if(fire){cow.fireTicks=160;if(cowTick%20==0)DamageCow(cow,1,Vector3.zero);}
        else if(cow.fireTicks>0){cow.fireTicks--;if(cowTick%20==0)DamageCow(cow,1,Vector3.zero);}
    }
    private void UpdateCows()
    {
        if(!Application.isPlaying || disabled || !IsWorldCreated || GenerationBusy)return;
        if(DropAuthority)
        {
            EnsureCowsRestored();
            if(!isSaving)
            {
                RefreshCowPlayers();cowClock=Mathf.Min(cowClock+Time.deltaTime,.25f);
                while(cowClock+.000001f>=.05f)
                {
                    cowClock=Mathf.Max(0,cowClock-.05f);cowTick=unchecked(cowTick+1);activeCows.Clear();
                    foreach(var cow in cows.Animals)if(CowAreaLoaded(cow.position)&&CowPlayerDistance(cow.position)<80*80)activeCows.Add(cow);
                    foreach(var cow in activeCows){TickCow(cow);if(cow.health>0&&cows.Get(cow.id)!=null&&cowTick%3==0)BroadcastCow(cow);}
                    cowSpawnClock+=.05f;
                    if(cowSpawnClock>=1){cowSpawnClock=0;SpawnNaturalCows();}
                }
            }
        }
        else if(!cowSyncRequested && NetworkClient.ready && NetworkClient.localPlayer!=null)
        {cowSyncRequested=true;NetworkClient.Send(new CowSyncRequest());}
        if(Instance==this&&(!NetworkServer.active || NetworkClient.active))UpdateCowVisuals();
    }
    private void UpdateCowVisuals()
    {
        cowVisualRemovals.Clear();
        foreach(var pair in cowVisuals)
        {
            var cow=cows.Get(pair.Key);
            if(cow==null ? !pair.Value.AnimateDeath(Time.deltaTime) : !CowAreaLoaded(cow.position))cowVisualRemovals.Add(pair.Key);
        }
        foreach(int id in cowVisualRemovals){cowVisuals[id].Dispose();cowVisuals.Remove(id);}
        foreach(var cow in cows.Animals)
        {
            bool visible=CowAreaLoaded(cow.position);
            if(!cowVisuals.TryGetValue(cow.id,out var visual) && visible)
            {
                if(!EnsureCowMeshes())continue;
                visual=new CowVisual(worldRenderer.transform,cow,cowMeshes,cowMaterial);cowVisuals.Add(cow.id,visual);
            }
            if(visual!=null){visual.root.SetActive(visible);if(visible)visual.Update(cow,this,Time.deltaTime);}
        }
    }
    private bool EnsureCowMeshes()
    {
        if(cowMaterial!=null)return true;
        var template=Resources.Load<Material>("CowMaterial");
        if(template==null||template.mainTexture==null)return false;
        cowMaterial=new Material(template);cowMeshes=new Mesh[CowModelGeometry.PartCount];
        float textureHeight=64f*template.mainTexture.height/template.mainTexture.width;
        for(int part=0;part<cowMeshes.Length;part++)
        {
            var data=CowModelGeometry.Build(part,textureHeight);var mesh=new Mesh{name="Cow part "+part};
            mesh.SetVertices(data.vertices);mesh.SetTriangles(data.triangles,0);
            var uv=new List<Vector2>();foreach(var p in data.uv)uv.Add(new Vector2(p.x,p.y));mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();cowMeshes[part]=mesh;
        }
        return true;
    }
    private void ResetCows(bool discardSave=false)
    {
        foreach(var visual in cowVisuals.Values)visual.Dispose();cowVisuals.Clear();
        if(cowMeshes!=null)foreach(var mesh in cowMeshes)if(mesh!=null)Destroy(mesh);cowMeshes=null;
        if(cowMaterial!=null)Destroy(cowMaterial);cowMaterial=null;
        cows.Restore(null);cowPopulatedChunks.Clear();activeCows.Clear();cowPlayers.Clear();cowSpawnCandidates.Clear();cowVisualRemovals.Clear();
        cowClock=cowSpawnClock=0;cowTick=0;cowSyncRequested=false;cowRandom=null;
        if(discardSave)cowRestore=null;
    }
}
