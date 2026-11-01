using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mirror;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

class Check
{
    static int checks;
    static void Assert(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Call(object obj,string method)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
    static Lobby NewLobby(ulong id=100, bool self=false)
    {
        var lobby=new Lobby(id);
        Lobby.states[id]=new LobbyState{owner=new Friend{Id=self?10UL:20UL,IsFriend=!self,IsMe=self,Name="Alex"}};
        lobby.SetData("project",LobbyManager.Project);lobby.SetData("protocol",LobbyManager.Protocol);
        lobby.SetData("state","ready");lobby.SetData("host",lobby.Owner.Id.ToString());
        lobby.SetData("seed","seed-json");lobby.SetData("betaSeed","-123456789123");lobby.SetData("time","3000");
        lobby.SetData("name","<b>Alex's game</b>");lobby.SetData("world","Test world");
        return lobby;
    }
    static void Reset()
    {
        LobbyManager.Cancel();LobbyManager.TakeError();Lobby.states.Clear();
        PlayerIdentity.SteamAvailable=true;NetworkManager.singleton=new NetworkManager();
        World.Overworld=null;MinecraftNetworkManager.failHost=false;SteamFriends.friends.Clear();SteamMatchmaking.created=null;
        MultiplayerMenuManager.Instance=null;Time.realtimeSinceStartup=0;
    }
    static MultiplayerMenuManager Browser()
    {
        var browser=new MultiplayerMenuManager();browser.gameObject.Add(new Menu());
        browser.lobbyContainer=new GameObject().transform;
        browser.lobbyPrefab=new GameObject();browser.lobbyPrefab.Add(new LobbyButton());
        for(int i=0;i<3;i++){var obj=new GameObject();if(i<2)obj.Add(new TextMeshProUGUI());obj.transform.SetParent(browser.lobbyPrefab.transform);}
        browser.textPrefab=new GameObject();browser.textPrefab.Add(new TextMeshProUGUI());browser.textPrefab.Add(new RectTransform());
        MultiplayerMenuManager.Instance=browser;return browser;
    }
    static async Task Run()
    {
        Reset();var lobby=NewLobby();Assert(LobbyManager.CanJoin(lobby),"ready friend's tagged lobby is joinable");
        lobby.SetData("project","other-app480-game");Assert(!LobbyManager.CanJoin(lobby),"unrelated App 480 games excluded");
        lobby.SetData("project",LobbyManager.Project);lobby.SetData("protocol","old");Assert(!LobbyManager.CanJoin(lobby),"incompatible protocol excluded");
        lobby.SetData("protocol",LobbyManager.Protocol);lobby.State.members=8;Assert(!LobbyManager.CanJoin(lobby),"full game excluded");
        lobby.State.members=1;lobby.SetData("state","starting");Assert(!LobbyManager.CanJoin(lobby),"loading host excluded");
        lobby.SetData("state","ready");lobby.SetData("host","30");Assert(!LobbyManager.CanJoin(lobby),"migrated/stale host excluded");
        lobby.SetData("host","20");lobby.State.owner=new Friend{Id=20,Name="Stranger"};Assert(!LobbyManager.CanJoin(lobby),"nonfriend owner excluded");

        Reset();lobby=NewLobby(self:true);SteamMatchmaking.create=Task.FromResult<Lobby?>(lobby);
        SteamMatchmaking.created=()=>Assert(LobbyManager.Hosting&&!NetworkClient.active,"hosting flag precedes Steam's lobby-entry callback");
        await LobbyManager.HostAsync("Beta",new Vector3Int());
        Assert(lobby.State.friendsOnly&&!lobby.State.joinable&&lobby.GetData("state")=="starting","friends-only lobby remains closed during loading");
        Assert(NetworkServer.active&&!NetworkServer.dontListen&&NetworkManager.singleton.clients==0,"host listens externally and never connects to itself");
        Assert(SteamMatchmaking.capacity==8&&lobby.GetData("betaSeed")=="123","capacity and numeric world seed published");
        LobbyManager.Tick();Assert(!lobby.State.joinable,"host isn't advertised before terrain exists");
        World.Overworld=new World{IsWorldCreated=true};LobbyManager.Tick();Assert(lobby.State.joinable&&lobby.GetData("state")=="ready","ready world becomes joinable");
        LobbyManager.Cancel();Assert(!lobby.State.joinable&&lobby.GetData("state")=="closed"&&lobby.State.leaves==1&&!LobbyManager.currentLobby.HasValue,"host exit closes and leaves lobby exactly once");

        Reset();lobby=NewLobby(self:true);SteamMatchmaking.create=Task.FromResult<Lobby?>(lobby);MinecraftNetworkManager.failHost=true;
        await LobbyManager.HostAsync("Failed",new Vector3Int());
        Assert(!LobbyManager.Hosting&&!LobbyManager.currentLobby.HasValue&&LobbyManager.LastError!=null,"host socket cleanup doesn't suppress the failure message");
        Reset();var create=new TaskCompletionSource<Lobby?>();SteamMatchmaking.create=create.Task;lobby=NewLobby(self:true);
        var hosting=LobbyManager.HostAsync("Cancelled",new Vector3Int());LobbyManager.Cancel();create.SetResult(lobby);await hosting;
        Assert(lobby.State.leaves==1&&!NetworkServer.active&&!LobbyManager.Hosting,"cancelled create cannot later host or leave a stale lobby");

        Reset();lobby=NewLobby();var join=LobbyManager.JoinAsync(lobby);await LobbyManager.JoinAsync(lobby);await join;
        Assert(lobby.State.joins==1&&NetworkManager.singleton.clients==1,"duplicate join clicks start one Steam/Mirror connection");
        Assert(NetworkManager.singleton.networkAddress=="20"&&WorldSettingsManager.Instance.betaWorldSeed==-123456789123&&WorldSettingsManager.Instance.worldTimeTicks==3000,"join targets host Steam ID with synchronized world settings");
        Assert(WorldSettingsManager.Instance.worldName=="Steam-100","remote world doesn't reuse local save caches");
        LobbyManager.Connected();Time.realtimeSinceStartup=100;LobbyManager.Tick();Assert(NetworkClient.active,"connected session doesn't hit connection timeout");
        LobbyManager.Disconnected();Assert(!LobbyManager.currentLobby.HasValue&&LobbyManager.LastError!=null,"host disconnect leaves lobby with usable error");

        Reset();lobby=NewLobby();await LobbyManager.JoinAsync(lobby);Time.realtimeSinceStartup=46;LobbyManager.Tick();
        Assert(!NetworkClient.active&&!LobbyManager.Joining&&lobby.State.leaves==1&&LobbyManager.LastError!=null,"unreachable host times out and cleans up");
        Reset();lobby=NewLobby();lobby.State.join=Task.FromResult(RoomEnter.Full);await LobbyManager.JoinAsync(lobby);
        Assert(NetworkManager.singleton.clients==0&&!LobbyManager.Joining&&LobbyManager.LastError!=null,"Steam lobby-entry failure doesn't start transport");
        Reset();lobby=NewLobby();var pending=new TaskCompletionSource<RoomEnter>();lobby.State.join=pending.Task;
        join=LobbyManager.JoinAsync(lobby);await Task.Delay(850);LobbyManager.Cancel();await LobbyManager.JoinAsync(lobby);
        Assert(lobby.State.joins==1,"cancelled native join cannot overlap retry to same lobby");
        pending.SetResult(RoomEnter.Success);await join;
        Assert(lobby.State.leaves==1&&NetworkManager.singleton.clients==0,"late entry after cancel leaves instead of starting client");
        Reset();lobby=NewLobby();pending=new TaskCompletionSource<RoomEnter>();lobby.State.join=pending.Task;
        join=LobbyManager.JoinAsync(lobby);await Task.Delay(850);lobby.State.owner=new Friend{Id=30,IsFriend=true};pending.SetResult(RoomEnter.Success);await join;
        Assert(lobby.State.leaves==1&&NetworkManager.singleton.clients==0,"owner swap between click and entry fails safely");
        Reset();lobby=NewLobby();lobby.SetData("seed","bad-json");await LobbyManager.JoinAsync(lobby);
        Assert(lobby.State.leaves==1&&NetworkManager.singleton.clients==0,"malformed settings leave entered lobby without client start");
        Reset();lobby=NewLobby();lobby.SetData("project","");join=LobbyManager.JoinAsync(lobby);
        await Task.Delay(200);Assert(lobby.State.joins==0,"uncached metadata doesn't immediately join or reject");
        lobby.SetData("project",LobbyManager.Project);await join;Assert(NetworkManager.singleton.clients==1,"delayed metadata can complete the join");
        Reset();lobby=NewLobby();lobby.SetData("project","");join=LobbyManager.JoinAsync(lobby);
        LobbyManager.Cancel();await join;Assert(lobby.State.joins==0&&!LobbyManager.Joining,"cancel during metadata refresh prevents a late join");
        Reset();PlayerIdentity.SteamAvailable=false;lobby=NewLobby();await LobbyManager.JoinAsync(lobby);
        Assert(lobby.State.joins==0&&!NetworkClient.active,"Steam-unavailable join stays offline");

        Reset();var browser=Browser();lobby=NewLobby();var unrelated=NewLobby(101);unrelated.SetData("project","spacewar");
        SteamFriends.friends.Add(new Friend{Id=20,IsFriend=true,IsPlayingThisGame=true,GameInfo=new Friend.FriendGameInfo{Lobby=lobby}});
        SteamFriends.friends.Add(new Friend{Id=21,IsFriend=true,IsPlayingThisGame=true,GameInfo=new Friend.FriendGameInfo{Lobby=lobby}});
        SteamFriends.friends.Add(new Friend{Id=22,IsFriend=true,IsPlayingThisGame=true,GameInfo=new Friend.FriendGameInfo{Lobby=unrelated}});
        SteamFriends.friends.Add(new Friend{Id=23,IsFriend=true,IsPlayingThisGame=true});
        browser.GetLobbys();await Task.Delay(1100);
        Assert(browser.lobbyContainer.childCount==1&&lobby.State.refreshes==1,"friends list deduplicates shared lobbies and excludes unrelated/menu-only friends");
        var row=browser.lobbyContainer.GetChild(0).gameObject;var button=row.GetComponent<LobbyButton>();
        Assert(row.GetComponentsInChildren<TextMeshProUGUI>().All(t=>!t.richText),"Steam text cannot inject formatting");
        button.SetBorder(true);browser.GetLobbys();await Task.Delay(1100);
        Assert(browser.selectedLobby!=null&&browser.selectedLobby.steamLobbyId==100,"refresh preserves selection by Steam lobby ID");
        button=browser.selectedLobby;Call(button,"OnDisable");Assert(browser.selectedLobby==null&&LobbyButton.currentLobbyButton==null,"disabled row clears selection references");
        SteamFriends.friends.Clear();browser.GetLobbys();await Task.Delay(1100);
        Assert(browser.lobbyContainer.GetChild(0).gameObject.GetComponentInChildren<TextMeshProUGUI>().text.StartsWith("No friends"),"empty friends list shows hosting guidance");
        browser.GetLobbys();browser.ShowMessage("Join failed");await Task.Delay(1100);
        Assert(browser.lobbyContainer.GetChild(0).gameObject.GetComponentInChildren<TextMeshProUGUI>().text=="Join failed","session errors invalidate an in-flight browser refresh");
        Reset();browser=Browser();browser.GetLobbys();Call(browser,"OnDisable");await Task.Delay(1100);
        Assert(browser.lobbyContainer.GetChild(0).gameObject.GetComponentInChildren<TextMeshProUGUI>().text.StartsWith("Looking"),"disabled browser ignores stale refresh result");
        Console.WriteLine(checks+" Steam friends/session checks passed (Unity, Mirror and native Steam mocked).");
    }
    static void Main(){Run().GetAwaiter().GetResult();}
}
