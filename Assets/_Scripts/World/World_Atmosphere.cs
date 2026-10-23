using Mirror;
using UnityEngine;

public partial class World
{
    [Header("Overworld sky")]
    public bool overworldFog=true,showClouds=true;
    public double worldTimeTicks=1000;
    public double CurrentWorldTicks => WorldServer.instance!=null&&NetworkClient.active?WorldServer.instance.WorldTicks:worldTimeTicks;
    private void AdvanceAtmosphereClock(float dt)
    {
        if(this!=Overworld||!Application.isPlaying||!IsWorldCreated)return;
        if(NetworkServer.active&&WorldServer.instance!=null)worldTimeTicks=WorldServer.instance.WorldTicks;
        else if(!NetworkClient.active)worldTimeTicks+=dt*20;
        skyLightReduction=Mathf.RoundToInt((1-SkyClock.Daylight(CurrentWorldTicks))*11);
    }
}
public partial class WorldServer
{
    [SyncVar] private double clockStartTicks=1000;
    [SyncVar] private double clockStartedAt;
    public double WorldTicks => clockStartTicks+System.Math.Max(0,NetworkTime.time-clockStartedAt)*20;
    private void InitializeWorldClock()
    {clockStartTicks=World.Overworld!=null?World.Overworld.worldTimeTicks:1000;clockStartedAt=NetworkTime.time;}
}
