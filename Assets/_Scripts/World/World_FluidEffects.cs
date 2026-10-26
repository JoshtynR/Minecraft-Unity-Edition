using Mirror;
using UnityEngine;

public partial class World
{
    private FluidEffects fluidEffects;
    public void EmitFluidEffect(Vector3Int position,byte kind)
    {
        // Each client observes hardening through ApplyFluidState. Avoid a second
        // RPC: the authoritative block delta already carries the event.
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        PlayFluidEffect((Vector3)position+Vector3.one*.5f,kind);
    }
    public void PlayFluidEffect(Vector3 position,byte kind)
    {
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=worldRenderer.gameObject.AddComponent<FluidEffects>();
        if(kind!=255)fluidEffects.Emit(position,kind);
    }
    public void PlayMobDeathSmoke(Vector3 position,float radius,float height)
    {
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=worldRenderer.gameObject.AddComponent<FluidEffects>();
        fluidEffects.EmitMobDeath(position,radius,height);
    }
    public void PlayBlockBreakParticles(Vector3Int position,BlockType type)
    {
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=worldRenderer.gameObject.AddComponent<FluidEffects>();
        fluidEffects.EmitBlockBreak(this,position,type);
    }
    public void PlayBlockHitParticles(Vector3Int position,BlockType type,Vector3 hitPoint)
    {
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=worldRenderer.gameObject.AddComponent<FluidEffects>();
        fluidEffects.EmitBlockHit(this,position,type,hitPoint);
    }
    public void PlaySprintParticles(Vector3 position,Vector3 horizontalVelocity,Block surface)
    {
        if(Instance!=this||Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=worldRenderer.gameObject.AddComponent<FluidEffects>();
        fluidEffects.EmitSprintDust(this,position,horizontalVelocity,surface);
    }
    private void DisposeFluidEffects(){if(fluidEffects!=null)Destroy(fluidEffects);fluidEffects=null;}
}
