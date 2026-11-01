using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[Serializable]
public sealed class PrimedTntState
{
    public int id,fuse=80;
    public Vector3 position,velocity;
    public PrimedTntState Copy()=>(PrimedTntState)MemberwiseClone();
}
public struct TntBatchMessage : NetworkMessage {public WorldDimension dimension;public PrimedTntState[] items;}
public struct ExplosionEffectMessage : NetworkMessage {public WorldDimension dimension;public Vector3 position;public float strength;}

public partial class World
{
    [NonSerialized] private List<PrimedTntState> runtimeTnt;
    private List<PrimedTntState> primedTnt=>runtimeTnt??(runtimeTnt=new List<PrimedTntState>());
    [NonSerialized] private Dictionary<int,PrimedTntVisual> runtimeTntVisuals;
    private Dictionary<int,PrimedTntVisual> tntVisuals=>runtimeTntVisuals??(runtimeTntVisuals=new Dictionary<int,PrimedTntVisual>());
    private int nextTntId=1;
    private float tntClock,tntSync;
    private bool TntAreaReady(Vector3 position)
    {
        // The boat/rider probe reserves headroom. TNT must also work on the
        // top build layer and during its small initial hop above that layer.
        if(position.y<0||position.y>worldHeight+64)return false;
        return VehicleAreaReady(new Vector3(position.x,Mathf.Clamp(position.y,0,worldHeight-3),position.z),.6f);
    }
    private void InitializeTnt()
    {
        NetworkClient.RegisterHandler<TntBatchMessage>(message=>{
            if(NetworkServer.active)return;
            var realm=FindDimension(message.dimension);if(realm==null)return;
            realm.primedTnt.Clear();if(message.items!=null)realm.primedTnt.AddRange(message.items);
        });
        NetworkClient.RegisterHandler<ExplosionEffectMessage>(message=>{
            if(!NetworkServer.active)FindDimension(message.dimension)?.PlayExplosionParticles(message.position,message.strength);
        });
    }
    public void EmitExplosionEffects(Vector3 center,float strength)
    {
        PlayExplosionParticles(center,strength);
        if(NetworkServer.active&&NetworkServer.connections.Count>(NetworkClient.active?1:0))
            NetworkServer.SendToAll(new ExplosionEffectMessage{dimension=dimension,position=center,strength=strength});
    }
    public bool PrimeTnt(Vector3Int position,bool chain=false)
    {
        if(!DropAuthority||disabled||isSaving||!IsWorldCreated||primedTnt.Count>=128||nextTntId==int.MaxValue||GetBlock(position).type!=BlockType.TNT||!TntAreaReady((Vector3)position+Vector3.one*.5f))return false;
        if(mobRandom==null)mobRandom=new JavaRandom(betaWorldSeed);
        SetFarmBlock(position,BlockType.Air);
        if(GetBlock(position).type!=BlockType.Air)return false;
        float angle=mobRandom.NextFloat()*(float)Math.PI*2;
        primedTnt.Add(new PrimedTntState{id=nextTntId++,fuse=chain?10+mobRandom.NextInt(20):80,
            position=(Vector3)position+new Vector3(.5f,0,.5f),velocity=new Vector3(-Mathf.Sin(angle)*.4f,4,-Mathf.Cos(angle)*.4f)});
        GameAudio.Broadcast("random/fuse",(Vector3)position+Vector3.one*.5f,1,1,this);tntSync=0;return true;
    }
    public PrimedTntState[] CaptureTnt()
    {var copy=new PrimedTntState[primedTnt.Count];for(int i=0;i<copy.Length;i++)copy[i]=primedTnt[i].Copy();return copy;}
    public void RestoreTnt(PrimedTntState[] saved)
    {
        ResetTnt(true);if(!DropAuthority||saved==null)return;
        var ids=new HashSet<int>();
        foreach(var item in saved)
        {
            if(item==null||item.id<=0||item.id==int.MaxValue||item.fuse<0||item.fuse>80||!VehiclePhysics.Finite(item.position)||!VehiclePhysics.Finite(item.velocity)||!ids.Add(item.id)||primedTnt.Count>=128)continue;
            primedTnt.Add(item.Copy());nextTntId=Math.Max(nextTntId,item.id+1);
        }
    }
    private void ResetTnt(bool discard)
    {
        if(runtimeTntVisuals!=null){foreach(var visual in tntVisuals.Values)if(visual!=null)Destroy(visual.gameObject);tntVisuals.Clear();}
        if(discard){runtimeTnt?.Clear();nextTntId=1;}
        tntClock=tntSync=0;
    }
    private void TickTnt()
    {
        int blasts=0;
        for(int i=primedTnt.Count-1;i>=0;i--)
        {
            var item=primedTnt[i];
            if(!TntAreaReady(item.position))continue;
            item.velocity.y-=.8f;
            var requested=item.velocity*.05f;
            if(TntAreaReady(item.position+requested))
            {
                var moved=BlockCollision.Resolve(this,item.position,.49f,.98f,requested,false);item.position+=moved;item.velocity*=.98f;
                if(Mathf.Abs(moved.x-requested.x)>.0001f)item.velocity.x=0;
                if(Mathf.Abs(moved.z-requested.z)>.0001f)item.velocity.z=0;
                if(moved.y>requested.y+.0001f){item.velocity.y*=-.5f;item.velocity.x*=.7f;item.velocity.z*=.7f;}
                else if(moved.y<requested.y-.0001f)item.velocity.y=0;
            }
            if(item.fuse>0)item.fuse--;
            if(item.fuse==0&&blasts<4)
            {
                // Remove before the blast can prime neighbouring blocks.
                primedTnt.RemoveAt(i);blasts++;
                RefreshMobPlayers();if(mobRandom==null)mobRandom=new JavaRandom(betaWorldSeed);
                ExplodeMob(item.position+Vector3.up*.49f,4);
            }
        }
    }
    private void UpdateTnt()
    {
        if(disabled||!IsWorldCreated||isSaving||System.Threading.Volatile.Read(ref betaPopulationJobs)!=0)return;
        if(DropAuthority)
        {
            tntClock=Mathf.Min(tntClock+Time.deltaTime,.15f);
            while(tntClock>=.05f){tntClock-=.05f;TickTnt();}
            tntSync-=Time.unscaledDeltaTime;
            if(tntSync<=0&&NetworkServer.active&&NetworkServer.connections.Count>(NetworkClient.active?1:0))
            {tntSync=.1f;NetworkServer.SendToAll(new TntBatchMessage{dimension=dimension,items=CaptureTnt()});}
        }
        if(Application.isBatchMode)return;
        foreach(var item in primedTnt)
        {
            bool ready=TntAreaReady(item.position);
            if(!tntVisuals.TryGetValue(item.id,out var visual)&&ready)
            {var root=new GameObject("Primed TNT");root.transform.SetParent(worldRenderer.transform,false);visual=root.AddComponent<PrimedTntVisual>();visual.Initialize();tntVisuals[item.id]=visual;}
            if(visual!=null){visual.gameObject.SetActive(ready);if(ready)visual.Step(this,item);}
        }
        int removed;
        do{removed=0;foreach(var entry in tntVisuals)if(!primedTnt.Exists(t=>t.id==entry.Key)){Destroy(entry.Value.gameObject);removed=entry.Key;break;}if(removed!=0)tntVisuals.Remove(removed);}while(removed!=0);
    }
}
