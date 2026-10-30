using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace UnityEngine
{
 public class HeaderAttribute:Attribute{public HeaderAttribute(string text){}}
 public static class Application{public static bool isPlaying=true;}
 public static class Time{public static float unscaledTime;}
 public static class Mathf{public static int RoundToInt(float value)=>(int)Math.Round(value);}
 public class WaitForSecondsRealtime{public float seconds;public WaitForSecondsRealtime(float value){seconds=value;}}
 public class Behaviour
 {
  public readonly List<IEnumerator> routines=new List<IEnumerator>();
  public void StartCoroutine(IEnumerator routine){if(routine.MoveNext())routines.Add(routine);}
  public void Resume(){foreach(var routine in routines.ToArray())if(!routine.MoveNext())routines.Remove(routine);}
 }
}
namespace Mirror
{
 public class SyncVarAttribute:Attribute{}
 public class ServerAttribute:Attribute{}
 public static class NetworkTime{public static double time;}
 public static class NetworkClient{public static bool active;}
 public static class NetworkServer{public static bool active;public static Dictionary<int,Connection> connections=new Dictionary<int,Connection>();}
 public class Connection{public Identity identity;}
 public class Identity{public Player player;public T GetComponent<T>() where T:class=>player as T;}
}
public enum WorldDimension{Overworld,Nether}
public partial class World
{
 public static World Overworld;public bool IsWorldCreated=true;public int skyLightReduction;
 public void Tick(float dt){AdvanceAtmosphereClock(dt);}
}
public partial class WorldServer:Behaviour
{
 public static WorldServer instance;
 public void Initialize(){InitializeWorldClock();}
}
public partial class Player:Behaviour
{
 public bool networkStarted,sleeping,DimensionTransferInProgress;public WorldDimension dimension;
 public int health=20,wakes;public bool morning;private double sleepStartedAt;private int sleepAttempt;
 private World world;
 public Player(World realm){world=realm;}
 public void Sleep(){sleeping=true;sleepStartedAt=SleepClock;BeginSleepCompletion(++sleepAttempt);}
 private void WakeFromBed(bool value){sleeping=false;sleepAttempt++;wakes++;morning=value;}
 public void ServerWakeFromBed(bool value){WakeFromBed(value);}
 public void Leave(){WakeFromBed(false);}
}
public static class Check
{
 static int count;
 static void Expect(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static World Setup(bool host)
 {
  NetworkTime.time=100;Time.unscaledTime=1000;NetworkClient.active=NetworkServer.active=host;
  NetworkServer.connections.Clear();World.Overworld=new World{worldTimeTicks=13000};
  WorldServer.instance=host?new WorldServer():null;if(host)WorldServer.instance.Initialize();return World.Overworld;
 }
 static Player Add(World world,int id,bool network=true)
 {
  var player=new Player(world){networkStarted=network};
  if(network)NetworkServer.connections[id]=new Connection{identity=new Identity{player=player}};
  return player;
 }
 public static void Main()
 {
  var world=Setup(true);var player=Add(world,0);player.Sleep();
  Expect(player.routines.Count==0&&WorldServer.instance.routines.Count==1,"solo host schedules server sleep only, never competing offline timer");
  NetworkTime.time+=4;WorldServer.instance.Resume();Expect(player.sleeping&&world.CurrentWorldTicks<24000,"host remains asleep until minimum sleep duration");
  NetworkTime.time+=1;WorldServer.instance.Resume();
  Expect(!player.sleeping&&player.morning&&player.wakes==1,"host wakes once at morning");
  Expect(world.CurrentWorldTicks==24000&&WorldServer.instance.WorldTicks==24000&&world.worldTimeTicks==24000,"sleep advances authoritative host clock AND saved world clock");
  NetworkTime.time+=1;world.Tick(.05f);Expect(world.CurrentWorldTicks==24020&&world.worldTimeTicks==24020,"next world update does not revert morning to night");
  world=Setup(false);player=Add(world,0,false);player.Sleep();Expect(player.routines.Count==1,"genuinely offline game has its own sleep timer");
  Time.unscaledTime+=5;Expect(player.FullyAsleep,"offline fully-asleep check uses the same clock as sleep start");player.Resume();
  Expect(!player.sleeping&&player.morning&&world.CurrentWorldTicks==24000,"offline timer advances its actual world clock");
  world=Setup(true);var first=Add(world,0);var second=Add(world,1);first.Sleep();
  Expect(WorldServer.instance.routines.Count==0,"one sleeping player cannot skip night while another is awake");
  NetworkTime.time+=2;second.Sleep();NetworkTime.time+=4;WorldServer.instance.Resume();
  Expect(first.sleeping&&second.sleeping&&world.CurrentWorldTicks<24000,"all players must finish entering sleep");
  NetworkTime.time+=1;WorldServer.instance.Resume();Expect(first.morning&&second.morning&&world.CurrentWorldTicks==24000,"all eligible players sleeping skips the authoritative clock together");
  world=Setup(true);first=Add(world,0);second=Add(world,1);first.Sleep();second.Sleep();first.Leave();NetworkTime.time+=5;WorldServer.instance.Resume();
  Expect(world.CurrentWorldTicks<24000&&second.sleeping,"leaving bed cancels the multiplayer skip");
  world=Setup(false);player=Add(world,0,false);player.Sleep();player.Leave();player.Resume();Expect(world.CurrentWorldTicks==13000,"cancelled offline timer cannot skip night");
  Console.WriteLine(count+" production sleep scheduling, quorum and authoritative-clock checks passed (Unity/Mirror mocked).");
 }
}
