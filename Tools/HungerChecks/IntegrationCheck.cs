using System;
using Mirror;
using UnityEngine;
namespace UnityEngine
{
 public class Range:Attribute {public Range(float min,float max){} }
 public static class Application {public static bool isPlaying=true;}
 public static class Time {public static float fixedDeltaTime=0.02f;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
 public class Transform {public Vector3 position;}
 public static class Mathf {public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Mirror
{
 public class SyncVar:Attribute {} public class Command:Attribute {}
 public static class NetworkTime {public static double time;}
}
public class World {public bool IsWorldCreated=true;public SurvivalDifficulty survivalDifficulty=SurvivalDifficulty.Normal;public bool naturalRegeneration;}
public class BaseEntity
{
 public Transform transform=new Transform();public float walkSpeed=4.317f,sprintSpeed=5.612f;public bool isGrounded=true,isSprinting,isFlying;public float actualStep=0.1f;
 public virtual void FixedUpdate(){transform.position=new Vector3(transform.position.x+actualStep,0,0);}
 protected virtual void OnJumpPerformed(){}
}
public partial class Player:BaseEntity
{
 public bool networkStarted,isLocalPlayer=true,isServer=true;public int health=20,hunger=20;public World world=new World();
 private void AdvanceMining(float dt){}
 public void JumpForTest(){OnJumpPerformed();}
 public void RemoteModes(bool sprint,bool fly,bool ground){CmdSurvivalMovement(sprint,fly,ground);}
 public void RemoteJump(bool sprint){CmdSurvivalJump(sprint);}
}
public static class IntegrationCheck
{
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Frames(Player p,int count){for(int i=0;i<count;i++){NetworkTime.time+=Time.fixedDeltaTime;p.FixedUpdate();}}
 public static void Main()
 {
  var host=new Player{networkStarted=true,isSprinting=true};Frames(host,50);
  Check(Math.Abs(host.exhaustion-0.5f)<0.015f,"host pays actual five-block sprint distance once");
  var blocked=new Player{networkStarted=true,isSprinting=true,actualStep=0};Frames(blocked,100);Check(blocked.exhaustion==0,"sprint into wall costs no distance exhaustion");
  var client=new Player{networkStarted=true,isServer=false,isSprinting=true};Frames(client,200);Check(client.hunger==20&&client.exhaustion==0,"client never drains replicated food");
  var starving=new Player{networkStarted=true,hunger=0,actualStep=0};Frames(starving,199);Check(starving.health==20,"fixed-step starvation waits four seconds");Frames(starving,1);Check(starving.health==19,"fixed-step starvation at exact four seconds");
  var low=new Player{hunger=6,isSprinting=true,actualStep=0};Frames(low,1);Check(!low.isSprinting,"controller stops sprint at food cutoff");
  var flying=new Player{isFlying=true,isSprinting=true};Frames(flying,200);flying.JumpForTest();Check(flying.exhaustion==0,"flight and flight jump exempt");
  host.JumpForTest();Check(Math.Abs(host.exhaustion-1.3f)<0.015f,"host jump not charged twice");
  var remote=new Player{networkStarted=true,isLocalPlayer=false};remote.RemoteModes(true,false,true);Frames(remote,3);remote.transform.position=new Vector3(0.5f,0,0);Frames(remote,2);
  Check(remote.transform.position.x==0.5f&&Math.Abs(remote.exhaustion-0.05f)<0.00001f,"server observes remote displacement without locally simulating it");
  remote.transform.position=new Vector3(1000,0,0);Frames(remote,3);Check(Math.Abs(remote.exhaustion-0.05f)<0.00001f,"teleport ignored by food distance");
  NetworkTime.time=10;remote.RemoteJump(true);float paid=remote.exhaustion;remote.RemoteJump(true);Check(remote.exhaustion==paid,"remote duplicate jump cooldown");
  var offline=new Player{networkStarted=false,isServer=false,hunger=0,actualStep=0};Frames(offline,200);Check(offline.health==19,"offline simulation works");
  var stopped=new Player{hunger=0,actualStep=0};stopped.world.IsWorldCreated=false;Frames(stopped,500);Check(stopped.health==20,"generation does not starve waiting player");
  Console.WriteLine(checks+" production player hunger authority/clock/movement/jump adapter checks passed (Unity/Mirror mocked).");
 }
}
