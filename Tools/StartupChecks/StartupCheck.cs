using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

static class StartupCheck
{
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Call(object target,string method)=>Lifecycle.Call(target,method);
 static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
 static AudioSource Active(MusicManager music)=>Field<bool>(music,"_usingA")?Field<AudioSource>(music,"_a"):Field<AudioSource>(music,"_b");
 static MusicManager Music(bool shuffle=false,bool loop=true,params AudioClip[] tracks)
 {
  var manager=new GameObject("Music").AddComponent<MusicManager>();manager.shuffle=shuffle;manager.loopPlaylist=loop;
  manager.menuTracks=new List<AudioClip>(tracks.Length>0?tracks:new[]{new AudioClip(),new AudioClip(),new AudioClip()});
  Call(manager,"Start");return manager;
 }
 static void Tick(MusicManager music,int frames=1)
 {
  for(int i=0;i<frames;i++)
  {
   Field<AudioSource>(music,"_a").Step(.1f);Field<AudioSource>(music,"_b").Step(.1f);
   Call(music,"Update");Scheduler.Step();
  }
 }
 static void End(MusicManager music){music.StopAll();UnityEngine.Object.Destroy(music.gameObject);Check(MusicManager.Instance==null,"destroyed singleton is released");}
 static void MusicChecks()
 {
  PlayerPrefs.SetFloat("musicVolume",25);var savedMusic=Music();Check(Math.Abs(savedMusic.volume-.25f)<.001&&Math.Abs(Active(savedMusic).volume-.25f)<.001,"saved music slider applies before playback starts");End(savedMusic);PlayerPrefs.SetFloat("musicVolume",100);
  var music=Music();Check(music.CurrentIndex==0&&music.IsPlaying,"ordered playlist starts at first clip");
  // This reproduces the old failure: outgoing track ends before Update, while fades are enabled.
  Active(music).Stop();Tick(music,1);
  Check(music.CurrentIndex==1&&music.IsPlaying&&Field<Coroutine>(music,"_fadeCo")==null,"already-finished clip advances immediately without repeatedly restarting a fade");
  Active(music).time=9;Tick(music,1);
  Check(music.CurrentIndex==2&&Field<Coroutine>(music,"_fadeCo")!=null,"crossfade begins before track finishes");
  Tick(music,20);Check(music.CurrentIndex==2&&Field<Coroutine>(music,"_fadeCo")==null&&Math.Abs(Active(music).volume-1)<.001,"crossfade completes once with incoming track at full volume");
  Active(music).time=9;Tick(music);Check(music.CurrentIndex==0,"last track wraps to first");Tick(music,20);
  music.Pause();int index=music.CurrentIndex;float time=Active(music).time;Tick(music,30);
  Check(!music.IsPlaying&&music.CurrentIndex==index&&Active(music).time==time,"pause never advances playlist");
  music.Play();Tick(music);Check(music.IsPlaying&&Active(music).time>time,"play resumes paused clip");
  music.Next();Tick(music,2);music.Pause();float gain=Active(music).volume;Tick(music,25);
  Check(Active(music).volume==gain&&Field<Coroutine>(music,"_fadeCo")!=null,"pausing freezes both sources and transition");
  music.Play();Tick(music,20);Check(Field<Coroutine>(music,"_fadeCo")==null&&music.IsPlaying,"both sources resume and fade completes");
  music.Next();Tick(music,2);music.PlayIndex(0,true);music.SetVolume(.35f);
  Check(Field<Coroutine>(music,"_fadeCo")==null&&Math.Abs(Active(music).volume-.35f)<.001,"hard cut cancels fade and volume remains adjustable");
  AudioListener.pause=true;index=music.CurrentIndex;Tick(music,30);Check(music.CurrentIndex==index,"listener pause does not advance tracks");AudioListener.pause=false;
  music.SetLoopTrack(true);Active(music).time=9.9f;Tick(music,30);
  Check(music.CurrentIndex==index&&music.IsPlaying&&Active(music).loop,"single-track loop uses audio-source looping");
  music.SetLoopTrack(false);music.StopAll();Tick(music,30);Check(!music.IsPlaying&&Scheduler.running.Count==0,"stop stays stopped and cancels coroutines");End(music);

  music=Music(false,false);music.PlayIndex(2,true);Active(music).time=9;Tick(music);
  Check(music.CurrentIndex==2&&music.IsPlaying,"non-looping final song is not cut off by crossfade lead time");
  Tick(music,15);Check(!music.IsPlaying&&music.CurrentClip==null,"non-looping playlist stops at end");End(music);

  music=Music(true,true);music.crossfadeSeconds=0;
  for(int pass=0;pass<20;pass++)
  {
   var heard=new HashSet<int>();
   for(int track=0;track<3;track++)
   {
    Check(heard.Add(music.CurrentIndex),"shuffle plays each entry once per pass");int old=music.CurrentIndex;
    Active(music).Stop();Tick(music);Check(music.CurrentIndex!=old,"shuffle avoids immediate repeat across pass boundaries");
   }
   Check(heard.Count==3,"shuffle pass includes every track");
  }
  End(music);
  music=Music(true,false);music.crossfadeSeconds=0;var visited=new HashSet<int>();
  for(int i=0;i<3;i++){visited.Add(music.CurrentIndex);Active(music).Stop();Tick(music);}
  Check(visited.Count==3&&!music.IsPlaying,"non-looping shuffled playlist ends after one complete pass");End(music);
  music=Music(false,true,new AudioClip(),null,new AudioClip());music.crossfadeSeconds=0;
  Check(music.CurrentIndex==0,"null entries do not prevent startup");music.Next();Check(music.CurrentIndex==2,"null entry is skipped");music.Next();Check(music.CurrentIndex==0,"null entry does not prevent wrapping");
  var standalone=new AudioClip();var before=music.CurrentClip;music.PlayClip(standalone,true);Check(music.CurrentClip==before,"unassigned clip cannot bypass scene playlist");End(music);
  music=Music(false,true,new AudioClip());music.crossfadeSeconds=0;for(int i=0;i<5;i++){Active(music).Stop();Tick(music);Check(music.CurrentIndex==0&&music.IsPlaying,"single-entry playlist repeats");}End(music);
  music=Music(false,true,(AudioClip)null);Tick(music,30);Check(!music.IsPlaying,"all-null playlist stays idle");End(music);
  music=Music();var menu=music.CurrentClip;var world=new AudioClip();music.worldTracks.Add(world);
  music.Next();Tick(music,1);Check(Field<Coroutine>(music,"_fadeCo")!=null,"scene transition test starts during a fade");
  SceneManager.LoadScene("World");
  Check(music.CurrentClip==world&&music.IsPlaying&&Field<Coroutine>(music,"_fadeCo")==null,"world scene cancels menu fade and starts world-only pool");
  var inactive=Field<bool>(music,"_usingA")?Field<AudioSource>(music,"_b"):Field<AudioSource>(music,"_a");
  Check(!inactive.isPlaying,"outgoing menu source is stopped");
  music.PlayClip(menu,true);Check(music.CurrentClip==world,"menu clip is rejected in world scene");
  music.Next();Tick(music,20);Check(music.CurrentClip==world,"world playlist loops only world tracks");
  music.Pause();SceneManager.LoadScene("MainMenu");Check(!music.IsPlaying&&music.menuTracks.Contains(music.CurrentClip),"scene switch preserves explicit pause");
  music.Play();music.worldTracks.Clear();SceneManager.LoadScene("World");Check(!music.IsPlaying&&music.CurrentClip==null,"empty world pool stays silent without menu fallback");
  SceneManager.LoadScene("MainMenu");Check(music.IsPlaying&&music.menuTracks.Contains(music.CurrentClip),"menu pool resumes after empty scene");
  SceneManager.LoadScene("Other");Check(!music.IsPlaying,"unsupported scene stays silent");
  SceneManager.LoadScene("MainMenu");music.StopAll();SceneManager.LoadScene("World");SceneManager.LoadScene("MainMenu");Check(!music.IsPlaying,"explicit stop remains stopped across scene changes");End(music);
  SceneManager.LoadScene("World");music=Music();music.worldTracks.Add(world);music.Play();Check(music.CurrentClip==world,"starting directly in world uses world pool");End(music);SceneManager.LoadScene("MainMenu");
 }
 static WorldLoadingScreen Screen()
 {
  return (WorldLoadingScreen)typeof(WorldLoadingScreen).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 }
 static void LoadingTick(int frame){Time.frameCount=frame;var view=Screen();if(view!=null)Call(view,"LateUpdate");}
 static void LoadingChecks()
 {
  for(int mask=0;mask<7;mask++)
  {
   var gate=new WorldLoadReadiness();Check(!gate.Advance((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,1000),"every readiness condition is required");
  }
  Resources.hud=new PlayerHudAssets{loadingBackground=new Texture2D(),guiWidgets=new Texture2D()};
  var preview=new GameObject("Preview").AddComponent<Camera>();GameObject.named["Rendering/Camera"]=preview.gameObject;
  var uiCamera=new GameObject("UI camera").AddComponent<Camera>();
  World.Instance=null;GameManager.Instance=null;SceneManager.LoadScene("MainMenu");WorldLoadingScreen.Begin();var original=Screen();
  Check(WorldLoadingScreen.IsLoading&&original.GetComponent<Canvas>().sortingOrder==32767,"persistent screen covers menu and game canvases");
  var background=original.transform.children.Single(c=>c.gameObject.name=="Background").GetComponent<UnityEngine.UI.Image>();
  var dirt=Field<UnityEngine.UI.RawImage>(original,"dirt");var fill=Field<RectTransform>(original,"progressFill");
  Check(background.color.a==1&&background.material==null&&dirt.color.a==1,"opaque default-material dirt backdrop cannot reveal the world");
  Check(dirt.texture==((PlayerHudAssets)Resources.hud).loadingBackground&&Math.Abs(dirt.uvRect.width-15)<.001,"dirt uses 32px tiles at native automatic GUI scale");
  Check(dirt.color.r==1&&dirt.color.g==1&&dirt.color.b==1,"loading dirt has no color tint");
  var shade=background.transform.children.Single(c=>c.gameObject.name=="BlackOverlay").GetComponent<UnityEngine.UI.Image>();
  Check(shade.color.r==0&&shade.color.g==0&&shade.color.b==0&&Math.Abs(shade.color.a-237/255f)<.0001f&&!shade.raycastTarget,"background uses a noninteractive black alpha-237 overlay");
  WorldLoadingScreen.ReportProgress("Building terrain",.5f);Check(fill.sizeDelta.x==50,"progress reflects reported generation work");
  WorldLoadingScreen.ReportProgress("Building terrain",.2f);Check(fill.sizeDelta.x==50,"late stage updates cannot move progress backwards");
  WorldLoadingScreen.ReportProgress("Done",2);Check(fill.sizeDelta.x==100,"progress is clamped to the native 100px bar");WorldLoadingScreen.ResetProgress();
  Check(fill.sizeDelta.x==0,"a new terrain generation resets progress");
  ScreenResize(original,dirt);
  Check(!original.transform.children.Any(c=>c.gameObject.name=="Cancel"),"loading screen does not create a cancel button");
  Check(preview.enabled,"world-camera suppression does not affect menu scenes");
  WorldLoadingScreen.Begin();Check(Screen()==original,"duplicate begin calls share one overlay");
  SceneManager.LoadScene("World");LoadingTick(10);Check(WorldLoadingScreen.IsLoading,"scene activation does not reveal game");
  Check(!preview.enabled&&uiCamera.enabled,"legacy isometric camera is disabled while the UI camera remains available");
  World.Instance=new World{IsWorldCreated=true};GameManager.Instance=new GameManager();LoadingTick(20);
  Check(WorldLoadingScreen.IsLoading,"generated map still waits for local player");
  var player=new GameObject("Player").AddComponent<Player>();player.inventory=new object();GameManager.Instance.localPlayer=player;GameManager.Instance.playerSpawned=true;
  LoadingTick(21);Check(WorldLoadingScreen.IsLoading,"spawned player waits for initialization and saved-data restoration");
  player.IsReadyForPlay=true;LoadingTick(22);Check(WorldLoadingScreen.IsLoading,"initialized player waits for rendered spawn chunk");
  var chunk=new GameObject("Chunk").AddComponent<ChunkRenderer>();chunk.ChunkData=new object();World.Instance.worldData.chunkDict[new Vector3Int()]=chunk;
  LoadingTick(23);LoadingTick(24);Check(WorldLoadingScreen.IsLoading,"ready world stays covered for two completed frames");
  player.IsReadyForPlay=false;LoadingTick(25);player.IsReadyForPlay=true;LoadingTick(26);LoadingTick(27);
  Check(WorldLoadingScreen.IsLoading,"deferred saved-player restoration resets readiness grace period");
  LoadingTick(28);Check(!WorldLoadingScreen.IsLoading&&Cursor.lockState==CursorLockMode.Locked&&!Cursor.visible,"ready world reveals and locks gameplay cursor");
  Mirror.NetworkClient.isLoadingScene=true;WorldLoadingScreen.Begin();LoadingTick(29);LoadingTick(32);
  Check(WorldLoadingScreen.IsLoading,"previous world's ready state cannot reveal a pending network scene transition");Mirror.NetworkClient.isLoadingScene=false;WorldLoadingScreen.Cancel();
  WorldLoadingScreen.Begin();LoadingTick(30);SceneManager.LoadScene("MainMenu");Check(!WorldLoadingScreen.IsLoading,"leaving world removes overlay");
  WorldLoadingScreen.Begin();WorldLoadingScreen.Cancel();Check(!WorldLoadingScreen.IsLoading,"disconnect cancels loading before world scene exists");
  SceneManager.LoadScene("World");WorldLoadingScreen.Begin();original=Screen();Mirror.NetworkManager.singleton=new Mirror.NetworkManager{offlineScene=""};
  WorldLoadingScreen.Cancel();Check(!WorldLoadingScreen.IsLoading,"programmatic disconnect cleanup still dismisses the loading screen");SceneManager.LoadScene("MainMenu");
  Application.isBatchMode=true;WorldLoadingScreen.Begin();Check(!WorldLoadingScreen.IsLoading,"headless server does not create loading UI");Application.isBatchMode=false;
  var network=new GameObject("Network").AddComponent<MinecraftNetworkManager>();
  network.OnServerDisconnect(null);Check(network.disconnects==1,"cancel before world-server creation still performs base disconnect");
  WorldServer.instance=new WorldServer();var connection=new Mirror.NetworkConnectionToClient();network.OnServerDisconnect(connection);
  Check(WorldServer.instance.saves==0&&WorldServer.instance.chestCloses==1&&network.disconnects==2,"cancel before player spawn releases sessions without dereferencing player");
  connection.identity=new GameObject("EarlyPlayer");var earlyPlayer=connection.identity.AddComponent<Player>();network.OnServerDisconnect(connection);
  Check(WorldServer.instance.saves==0,"cancel before player Start does not save uninitialized state");
  earlyPlayer.inventory=new object();network.OnServerDisconnect(connection);Check(WorldServer.instance.saves==1,"ordinary disconnect still saves initialized player");
  network.OnClientConnect();Check(WorldLoadingScreen.IsLoading,"actual client-connect hook opens persistent loading screen");
  network.OnClientDisconnect();Check(!WorldLoadingScreen.IsLoading,"actual client-disconnect hook clears loading screen");
  Resources.hud=null;GameObject.named.Clear();
 }
 static void ScreenResize(WorldLoadingScreen view,UnityEngine.UI.RawImage dirt)
 {
  UnityEngine.Screen.width=1280;UnityEngine.Screen.height=720;LoadingTick(0);
  Check(view.GetComponent<UnityEngine.UI.CanvasScaler>().scaleFactor==3&&Math.Abs(dirt.uvRect.width-1280/3f/32)<.001,"resizing preserves native GUI scale and dirt tile size");
  UnityEngine.Screen.width=1920;UnityEngine.Screen.height=1080;LoadingTick(1);
 }
 public static void Main(){MusicChecks();LoadingChecks();Console.WriteLine(checks+" music transition/shuffle/pause and world/player readiness checks passed (Unity mocked).");}
}
