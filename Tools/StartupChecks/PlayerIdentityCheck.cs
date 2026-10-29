using System;
using System.Collections;

namespace Mirror
{
    public static class NetworkServer {public static bool active;}
    public static class NetworkClient
    {
        public static bool active,ready,isConnected,isLoadingScene;
        public static object connection;
    }
}
public static class PlayerIdentity
{
    public static string DisplayName="Solo player";
    public static ulong Id=42;
}
public partial class Player
{
    public string PlayerName;public ulong SteamId;
    public bool isLocalPlayer=true;
    public uint netId=1;
    public int sent;
    public string receivedName;
    public ulong receivedId;
    public void CmdSetPlayerServerData(string name,ulong id)
    {
        if(!Mirror.NetworkClient.ready || !Mirror.NetworkClient.isConnected || Mirror.NetworkClient.connection==null)
            throw new Exception("Command sent before ready");
        sent++;receivedName=name;receivedId=id;
    }
    static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    static void Connect()
    {
        Mirror.NetworkClient.active=true;Mirror.NetworkClient.ready=false;
        Mirror.NetworkClient.isConnected=true;Mirror.NetworkClient.isLoadingScene=false;
        Mirror.NetworkClient.connection=new object();
    }
    public static void Main()
    {
        Connect();var player=new Player();var routine=player.PublishPlayerIdentityWhenReady();
        Check(routine.MoveNext()&&player.sent==0,"solo host waits rather than dropping command");
        Mirror.NetworkClient.ready=true;
        Check(!routine.MoveNext()&&player.sent==1,"sends exactly once when ready");
        Check(player.receivedName==PlayerIdentity.DisplayName&&player.receivedId==42,"identity preserved");
        Check(!player.PublishPlayerIdentityWhenReady().MoveNext()&&player.sent==1,"repeat startup cannot duplicate publication");
        Connect();player=new Player();routine=player.PublishPlayerIdentityWhenReady();
        Mirror.NetworkClient.ready=true;Mirror.NetworkClient.connection=null;
        Check(routine.MoveNext()&&player.sent==0,"ready flag alone is insufficient");
        Mirror.NetworkClient.connection=new object();Mirror.NetworkClient.isConnected=false;
        Check(routine.MoveNext()&&player.sent==0,"waits for connected state");
        Mirror.NetworkClient.isConnected=true;Mirror.NetworkClient.isLoadingScene=true;
        Check(routine.MoveNext()&&player.sent==0,"scene transition defers identity");
        Mirror.NetworkClient.isLoadingScene=false;player.netId=0;
        Check(routine.MoveNext()&&player.sent==0,"waits for spawned network identity");
        player.netId=1;
        Check(!routine.MoveNext()&&player.sent==1,"remote client resumes after transition");
        Connect();player=new Player();routine=player.PublishPlayerIdentityWhenReady();routine.MoveNext();
        Mirror.NetworkClient.active=false;
        Check(!routine.MoveNext()&&player.sent==0,"disconnect cancels pending publication");
        Connect();player=new Player();routine=player.PublishPlayerIdentityWhenReady();routine.MoveNext();player.isLocalPlayer=false;
        Check(!routine.MoveNext()&&player.sent==0,"loss of local ownership cancels publication");
        Connect();Mirror.NetworkServer.active=true;player=new Player();routine=player.PublishPlayerIdentityWhenReady();
        Check(!routine.MoveNext()&&player.sent==0&&player.PlayerName==PlayerIdentity.DisplayName&&player.SteamId==PlayerIdentity.Id,"host identity is applied directly while NotReady");
        Mirror.NetworkServer.active=false;
        Console.WriteLine("12 player-identity startup/readiness checks passed (Mirror mocked).");
    }
}
