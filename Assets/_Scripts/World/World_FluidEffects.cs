using Mirror;
using UnityEngine;

public partial class World
{
    private FluidEffects fluidEffects;
    public void EmitFluidEffect(Vector3Int position,byte kind)
    {
        // Each client observes hardening through ApplyFluidState. Avoid a second
        // RPC: the authoritative block delta already carries the event.
        if(Application.isBatchMode||!Application.isPlaying)return;
        PlayFluidEffect((Vector3)position+Vector3.one*.5f,kind);
    }
    public void PlayFluidEffect(Vector3 position,byte kind)
    {
        if(Application.isBatchMode||!Application.isPlaying)return;
        if(fluidEffects==null)fluidEffects=gameObject.AddComponent<FluidEffects>();
        if(kind!=255)fluidEffects.Emit(position,kind);
    }
    private void DisposeFluidEffects(){if(fluidEffects!=null)Destroy(fluidEffects);fluidEffects=null;}
}
