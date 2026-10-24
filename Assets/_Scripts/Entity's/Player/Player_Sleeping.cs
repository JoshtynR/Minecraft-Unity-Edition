using System.Collections;
using Mirror;
using UnityEngine;

public partial class Player
{
    private const byte BedOccupied=4;
    [SyncVar] public bool sleeping;
    [SyncVar] private Vector3Int sleepingBed;
    [SyncVar] private Vector3Int sleepingFoot;
    [SyncVar] private bool hasBedSpawn;
    [SyncVar] private Vector3Int bedSpawn;
    private Vector3 sleepReturnPosition;
    private int sleepHealth;
    private int sleepAttempt;
    private double sleepStartedAt;
    private string sleepMessage;
    private float sleepMessageUntil;
    private bool sleepCursorReleased;
    private CursorLockMode sleepCursorLock;
    private bool sleepCursorVisible;
    private GameObject sleepGameUI;
    private bool sleepGameUIWasActive;
    public string SleepMessage=>Time.unscaledTime<sleepMessageUntil?sleepMessage:string.Empty;
    private Vector3 SavedSleepPosition=>sleeping?sleepReturnPosition:transform.position;
    internal bool FullyAsleep=>sleeping&&NetworkTime.time-sleepStartedAt>=5;
    public Vector3 SleepingDirection
    {
        get
        {
            var direction=sleepingFoot-sleepingBed;
            return direction==Vector3Int.zero?Vector3.forward:(Vector3)direction;
        }
    }

    private bool TryUseBed(Block block)
    {
        if(block==null||block.type!=BlockType.Bed||dimension!=WorldDimension.Overworld)return false;
        if(networkStarted)CmdUseBed(block.globalWorldPosition);
        else UseBed(block.globalWorldPosition);
        return true;
    }

    [Command] private void CmdUseBed(Vector3Int position){UseBed(position);}

    private void UseBed(Vector3Int position)
    {
        if(sleeping||dimensionTransfer||dimension!=WorldDimension.Overworld||health<=0||world==null||world.isSaving)return;
        var reachDelta=transform.position-((Vector3)position+Vector3.one*.5f);
        if(Mathf.Abs(reachDelta.x)>3||Mathf.Abs(reachDelta.y)>2||Mathf.Abs(reachDelta.z)>3)return;
        var clicked=world.GetBlock(position);
        if(clicked==null||clicked.type!=BlockType.Bed)return;
        byte clickedState=SpecialBlockGeometry.State(BlockType.Bed,clicked.blockShape);
        Vector3Int head=(clickedState&8)!=0?position:position+SpecialBlockGeometry.PartnerOffset(BlockType.Bed,clickedState);
        var headBlock=world.GetBlock(head);
        if(headBlock==null||headBlock.type!=BlockType.Bed){SendSleepMessage("This bed is incomplete");return;}
        byte headState=SpecialBlockGeometry.State(BlockType.Bed,headBlock.blockShape);
        Vector3Int foot=head+SpecialBlockGeometry.PartnerOffset(BlockType.Bed,headState);
        var footBlock=world.GetBlock(foot);
        if(footBlock==null||footBlock.type!=BlockType.Bed){SendSleepMessage("This bed is incomplete");return;}
        byte footState=SpecialBlockGeometry.State(BlockType.Bed,footBlock.blockShape);
        if((headState&BedOccupied)!=0||(footState&BedOccupied)!=0)
        {
            if(BedInUse(head)){SendSleepMessage("This bed is occupied");return;}
            // Occupancy is stored in classic bed metadata. Clear stale bits
            // left by a disconnect or interrupted save before accepting use.
            SetBedOccupied(world,head,false);SetBedOccupied(world,foot,false);
        }
        if(!SleepingAtNight(world.CurrentWorldTicks)){SendSleepMessage("You can only sleep at night");return;}
        Vector3 center=((Vector3)head+(Vector3)foot)*.5f+new Vector3(.5f,.5f,.5f);
        if(world.HasHostileMobNear(center,8,5)){SendSleepMessage("You may not rest now; there are monsters nearby");return;}

        CancelFoodForTravel();serverMining=localMining=false;
        hasBedSpawn=true;bedSpawn=head;sleepingBed=head;sleepingFoot=foot;sleepReturnPosition=transform.position;sleepHealth=health;
        sleepStartedAt=networkStarted?NetworkTime.time:Time.unscaledTime;sleeping=true;int attempt=++sleepAttempt;
        SetBedOccupied(world,head,true);SetBedOccupied(world,foot,true);
        var liePosition=(Vector3)head+new Vector3(.5f,9f/16f,.5f);
        MoveForSleep(liePosition);
        if(networkStarted)TargetEnterBed(connectionToClient,liePosition);
        else {EnterSleepClient(liePosition);StartCoroutine(CompleteOfflineSleep(attempt));}
        if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.EvaluateSleep();
    }

    private static bool SleepingAtNight(double ticks)
    {
        double time=(ticks%SkyClock.DayTicks+SkyClock.DayTicks)%SkyClock.DayTicks;
        return time>=12541&&time<=23458;
    }

    private bool BedInUse(Vector3Int head)
    {
        if(NetworkServer.active)
            foreach(var connection in NetworkServer.connections.Values)
            {
                if(connection==null||connection.identity==null)continue;
                var player=connection.identity.GetComponent<Player>();
                if(player!=null&&player!=this&&player.sleeping&&player.dimension==dimension&&player.sleepingBed==head)return true;
            }
        if(!NetworkServer.active&&GameManager.Instance!=null)
            foreach(var player in GameManager.Instance.players)
                if(player!=null&&player!=this&&player.sleeping&&player.dimension==dimension&&player.sleepingBed==head)return true;
        return false;
    }

    private IEnumerator CompleteOfflineSleep(int attempt)
    {
        // Classic clients spend 100 ticks entering bed before the night skips.
        yield return new WaitForSecondsRealtime(5f);
        if(!sleeping||attempt!=sleepAttempt)yield break;
        double now=world.CurrentWorldTicks,time=(now%SkyClock.DayTicks+SkyClock.DayTicks)%SkyClock.DayTicks;
        world.worldTimeTicks=now+(SkyClock.DayTicks-time);
        WakeFromBed(true);
    }

    public void RequestWakeFromBed()
    {
        if(!sleeping)return;
        if(networkStarted&&!isServer)CmdWakeFromBed();else WakeFromBed(false);
    }
    [Command] private void CmdWakeFromBed(){WakeFromBed(false);}

    [Server] internal void ServerWakeFromBed(bool morning){WakeFromBed(morning);}
    private void WakeFromBed(bool morning)
    {
        if(!sleeping)return;
        var realm=CurrentWorld;Vector3 destination=sleepReturnPosition;
        if(realm!=null)
        {
            Vector3 safe;
            if(WorldSpawnRules.TryResolveBed(realm,sleepingBed,out safe))destination=safe;
            ClearOccupiedBed(realm,sleepingBed,sleepingFoot);
        }
        sleeping=false;sleepAttempt++;MoveForSleep(destination);
        if(networkStarted)TargetLeaveBed(connectionToClient,destination,morning);
        else {RestoreSleepCursor();if(morning)ShowSleepMessage("Respawn point set");}
    }

    private void TickSleeping()
    {
        if(!sleeping)return;
        var realm=CurrentWorld;var head=realm==null?null:realm.GetBlock(sleepingBed);var foot=realm==null?null:realm.GetBlock(sleepingFoot);
        if((isServer||!networkStarted)&&(health!=sleepHealth||head==null||head.type!=BlockType.Bed||foot==null||foot.type!=BlockType.Bed))WakeFromBed(false);
        else if((isServer||!networkStarted)&&!SleepingAtNight(realm.CurrentWorldTicks))WakeFromBed(true);
        horizontal=vertical=verticalMomentum=0;jumpRequest=false;isSprinting=false;
        falling.Reset();ResetWalkMotion();
    }

    private void MoveForSleep(Vector3 position)
    {
        transform.position=position;velocity=extraForce=Vector3.zero;verticalMomentum=0;
        var sync=GetComponent<NetworkTransform>();if(NetworkServer.active&&sync!=null)sync.RpcTeleport(position);
    }

    private void SetBedOccupied(World realm,Vector3Int position,bool occupied)
    {
        var block=realm.GetBlock(position);if(block==null||block.type!=BlockType.Bed)return;
        byte state=SpecialBlockGeometry.State(BlockType.Bed,block.blockShape);
        state=occupied?(byte)(state|BedOccupied):(byte)(state&~BedOccupied);
        realm.SetBlock(position,BlockType.Bed,state);
        if(NetworkServer.active&&WorldServer.instance!=null)WorldServer.instance.BroadcastBlock(realm,position,BlockType.Bed,state);
    }

    private void ClearOccupiedBed(World realm,Vector3Int head,Vector3Int foot)
    {
        SetBedOccupied(realm,head,false);SetBedOccupied(realm,foot,false);
    }

    private void SendSleepMessage(string message)
    {
        if(networkStarted&&connectionToClient!=null)TargetSleepMessage(connectionToClient,message);
        else ShowSleepMessage(message);
    }
    [TargetRpc] private void TargetSleepMessage(NetworkConnection target,string message){ShowSleepMessage(message);}
    [TargetRpc] private void TargetEnterBed(NetworkConnection target,Vector3 position){EnterSleepClient(position);}
    [TargetRpc] private void TargetLeaveBed(NetworkConnection target,Vector3 position,bool morning)
    {sleeping=false;MoveForSleep(position);RestoreSleepCursor();if(morning)ShowSleepMessage("Respawn point set");}
    private void EnterSleepClient(Vector3 position)
    {
        MoveForSleep(position);ShowSleepMessage("Respawn point set");
        sleepGameUI=GameObject.Find("PlayerCanvas/GameUI");
        if(sleepGameUI!=null){sleepGameUIWasActive=sleepGameUI.activeSelf;sleepGameUI.SetActive(false);}
        if(sleepCursorReleased)return;
        sleepCursorLock=Cursor.lockState;sleepCursorVisible=Cursor.visible;sleepCursorReleased=true;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    private void RestoreSleepCursor()
    {
        if(sleepGameUI!=null){sleepGameUI.SetActive(sleepGameUIWasActive);sleepGameUI=null;}
        if(!sleepCursorReleased)return;sleepCursorReleased=false;
        Cursor.lockState=sleepCursorLock;Cursor.visible=sleepCursorVisible;
    }
    private void ShowSleepMessage(string message){sleepMessage=message;sleepMessageUntil=Time.unscaledTime+3;}
}

public partial class WorldServer
{
    private bool sleepCompletionPending;
    [Server] public void EvaluateSleep()
    {
        if(sleepCompletionPending)return;
        int awake=0,asleep=0;
        foreach(var connection in NetworkServer.connections.Values)
        {
            if(connection==null||connection.identity==null)continue;
            var player=connection.identity.GetComponent<Player>();
            if(player==null||player.dimension!=WorldDimension.Overworld||player.health<=0||player.DimensionTransferInProgress)continue;
            if(player.sleeping)asleep++;else awake++;
        }
        if(asleep>0&&awake==0)StartCoroutine(CompleteNetworkSleep());
    }

    private IEnumerator CompleteNetworkSleep()
    {
        sleepCompletionPending=true;yield return new WaitForSecondsRealtime(5f);
        try
        {
            var sleepers=new System.Collections.Generic.List<Player>();
            while(true)
            {
                sleepers.Clear();bool ready=true;
                foreach(var connection in NetworkServer.connections.Values)
                {
                    if(connection==null||connection.identity==null)continue;
                    var player=connection.identity.GetComponent<Player>();
                    if(player==null||player.dimension!=WorldDimension.Overworld||player.health<=0||player.DimensionTransferInProgress)continue;
                    if(!player.sleeping)yield break;
                    sleepers.Add(player);if(!player.FullyAsleep)ready=false;
                }
                if(sleepers.Count==0)yield break;
                if(ready)break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            double now=WorldTicks,time=(now%SkyClock.DayTicks+SkyClock.DayTicks)%SkyClock.DayTicks;
            SetWorldTime(now+(SkyClock.DayTicks-time));
            foreach(var player in sleepers)player.ServerWakeFromBed(true);
        }
        finally{sleepCompletionPending=false;EvaluateSleep();}
    }
}
