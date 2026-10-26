using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

static class AudioCheck
{
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static AudioClip Clip(string name)=>new AudioClip{name=name};
 static GameAudio Pool()=>(GameAudio)typeof(GameAudio).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
 static AudioSource[] Voices()=>(AudioSource[])typeof(GameAudio).GetField("voices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(Pool());
 static void Tick(PlayerGameplayAudio observer)=>Lifecycle.Call(observer,"LateUpdate");
 static void StopVoices(){if(Pool()!=null)foreach(var voice in Voices())voice.Stop();}
 public static void Main()
 {
  Check(GameAudio.Matches("stone1","stone")&&GameAudio.Matches("STONE12","stone")&&GameAudio.Matches("stone","stone"),"bare and numbered sound variants match");
  Check(!GameAudio.Matches("stone_bricks1","stone")&&!GameAudio.Matches("stone1_extra","stone"),"unrelated sound stems are excluded");
  Resources.audio["Sounds/step"]=new[]{Clip("stone1"),Clip("stone2"),Clip("grass1")};
  Resources.audio["Sounds/step 1"]=new[]{Clip("sand1"),Clip("snow1"),Clip("cloth1"),Clip("ladder1")};
  Resources.audio["Sounds/random"]=new[]{Clip("glass1"),Clip("classic_hurt"),Clip("levelup")};
  Check(GameAudio.Find("step/sand").name=="sand1","alternate step 1 directory is used when canonical family is absent");
  int reads=Resources.reads;for(int i=0;i<20;i++)GameAudio.Find("step/sand");Check(Resources.reads==reads,"sound lookup caches directory and family results");
  Check(GameAudio.Find("missing/sound")==null,"missing clip is silent");
  foreach(BlockType block in Enum.GetValues(typeof(BlockType)))
  {
   string material=BlockSoundRules.Material(block);
   if(material!=null)Check(new[]{"cloth","grass","gravel","sand","snow","stone","wood"}.Contains(material),"every block uses a supported surface family");
  }
  Check(BlockSoundRules.Break(BlockType.Glass)=="random/glass"&&BlockSoundRules.Place(BlockType.Glass)=="dig/stone","glass breaks with glass sound and places with solid sound");
  Check(BlockSoundRules.Material(BlockType.Lava)==null&&BlockSoundRules.Material(BlockType.Water)==null,"fluids do not use solid-block placement sounds");
  Check(BlockSoundRules.Use(BlockType.WoodenDoor,4)=="random/door_open"&&BlockSoundRules.Use(BlockType.Trapdoor,0)=="random/door_close","door sounds follow open state");
  var clip=Clip("test");GameAudio.PlayClip(clip,new Vector3(1,0,0),.5f);
  var first=Voices().Single(v=>v.isPlaying);GameAudio.PlayClip(clip,new Vector3(5,0,0),.5f);
  Check(Voices().Count(v=>v.isPlaying)==2&&first.transform.position.x==1,"overlapping audio voices retain independent world positions");
  var pool=Pool();for(int i=0;i<80;i++)GameAudio.PlayClip(clip,new Vector3(i,0,0),2,true,3);
  Check(Pool()==pool&&Voices().Length==24&&pool.transform.children.Count==24,"sound bursts reuse a bounded pool");
  Check(Voices().All(v=>v.volume<=1&&v.pitch<=2&&v.dopplerLevel==0),"voice settings clamp levels and disable movement doppler");
  StopVoices();WorldLoadingScreen.IsLoading=true;GameAudio.PlayClip(clip,Vector3.zero,1);
  Check(!Voices().Any(v=>v.isPlaying),"world audio is suppressed while loading");
  GameAudio.PlayClip(clip,Vector3.zero,.2f,false);Check(Voices().Single(v=>v.isPlaying).spatialBlend==0,"UI remains audible in 2D during loading");WorldLoadingScreen.IsLoading=false;
  StopVoices();Camera.main=new GameObject("Camera").AddComponent<Camera>();GameAudio.PlayClip(clip,new Vector3(100,0,0),1);
  Check(!Voices().Any(v=>v.isPlaying),"distant sounds do not consume voices");Camera.main=null;
  World.Instance=new World{IsWorldCreated=true};var player=new GameObject("Player").AddComponent<Player>();player.isLocalPlayer=true;
  GameAudio.BroadcastBlock(BlockType.Stone,new Vector3Int(1,2,3),true,World.Instance);Check(World.Instance.breakParticles==1,"offline block breaking triggers one local debris burst");StopVoices();
  GameAudio.BroadcastBlock(BlockType.Stone,new Vector3Int(1,2,3),false,World.Instance);Check(World.Instance.breakParticles==1,"block placement does not trigger destruction debris");StopVoices();
  WorldServer.instance=new WorldServer();Mirror.NetworkServer.active=true;GameAudio.BroadcastBlock(BlockType.Stone,new Vector3Int(1,2,3),true,World.Instance);
  Check(World.Instance.breakParticles==2,"host block breaking broadcasts exactly one debris burst");Mirror.NetworkServer.active=false;WorldServer.instance=null;StopVoices();
  var observer=player.gameObject.AddComponent<PlayerGameplayAudio>();Tick(observer);Check(!Voices().Any(v=>v.isPlaying),"spawning player does not trigger a step or damage sound");
  player.transform.position=new Vector3(1.8f,0,0);Tick(observer);Check(Voices().Single(v=>v.isPlaying).clip.name.StartsWith("stone"),"walking plays sound for the supporting block");
  StopVoices();player.isCrouching=true;player.transform.position=new Vector3(3.6f,0,0);Tick(observer);Check(!Voices().Any(v=>v.isPlaying),"sneaking is silent");player.isCrouching=false;
  player.transform.position=new Vector3(100,0,0);Tick(observer);Check(!Voices().Any(v=>v.isPlaying),"teleport does not generate a burst of footsteps");
  player.health=18;Tick(observer);Check(Voices().Single(v=>v.isPlaying).clip.name=="classic_hurt","health loss plays classic player hurt sound");
  StopVoices();Tick(observer);Check(!Voices().Any(v=>v.isPlaying),"unchanged health never repeats hurt sound");
  player.health=20;Tick(observer);Check(!Voices().Any(v=>v.isPlaying),"healing does not play damage sound");
  player.experienceLevel=1;Tick(observer);Check(Voices().Single(v=>v.isPlaying).clip.name=="levelup","experience level increase plays level-up sound");
  UnityEngine.Object.Destroy(pool.gameObject);Check(Pool()==null&&!clip.destroyed,"pool cleanup preserves shared audio clips");
  Application.isBatchMode=true;GameAudio.Play("step/stone",Vector3.zero);Check(Pool()==null,"dedicated server creates no audio voices");Application.isBatchMode=false;
  Console.WriteLine(checks+" sound matching/material mapping/pooling/player-event checks passed (Unity mocked).");
 }
}
