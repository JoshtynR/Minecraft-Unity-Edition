using System;using System.Collections.Generic;using System.Reflection;using System.Threading;using System.Threading.Tasks;using UnityEngine;
public static class DimensionCheck {
 static int checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
 static World Realm(WorldDimension dimension){var root=new GameObject(dimension.ToString());var w=root.AddComponent<World>();w.dimension=dimension;w.secondaryDimension=dimension!=WorldDimension.Overworld;w.worldRenderer=new GameObject("Chunks").AddComponent<WorldRenderer>();w.worldData=new WorldData{worldName="test",owner=w,chunkDataDict=new Dictionary<Vector3Int,ChunkData>(),chunkDict=new Dictionary<Vector3Int,ChunkRenderer>()};w.Awake();return w;}
 static void Frame(World w,Vector3Int bottom,Vector3Int axis,bool lit){for(int u=-1;u<=2;u++)for(int v=-1;v<=3;v++){bool edge=u==-1||u==2||v==-1||v==3;if((u==-1||u==2)&&(v==-1||v==3))continue;var p=bottom+axis*u+Vector3Int.up*v;w.blocks[p]=new Block{type=edge?BlockType.Obsidian:lit?BlockType.NetherPortal:BlockType.Air,chunkData=new ChunkData{worldRef=w}};}}
 static void Wait(Func<bool> condition){var until=DateTime.UtcNow.AddSeconds(5);while(!condition()){if(DateTime.UtcNow>until)throw new Exception("Transfer timed out");Thread.Sleep(1);}}
 public static void Main(){
  Check(DimensionRules.StorageName("existing",WorldDimension.Overworld)=="existing","Legacy save location");
  Check(DimensionRules.StorageName("existing",WorldDimension.Nether)=="existing/DIM-1","Separate Nether location");
  Check(DimensionRules.Valid((WorldDimension)70)==WorldDimension.Overworld,"Invalid saved dimension migration");
  var scaled=DimensionRules.Destination(new Vector3(-80,72,160),WorldDimension.Nether);Check(scaled.x==-10&&scaled.z==20&&scaled.y==72,"8:1 signed travel");
  var back=DimensionRules.Destination(scaled,WorldDimension.Overworld);Check(back.x==-80&&back.z==160,"Round trip coordinates");
  var clamped=DimensionRules.Destination(new Vector3(4000000,72,-4000000),WorldDimension.Overworld);Check(clamped.x<=29999872&&clamped.z>=-29999872,"World border clamp");
  var overworld=Realm(WorldDimension.Overworld);var nether=Realm(WorldDimension.Nether);World.Instance=overworld;
  foreach(var axis in new[]{Vector3Int.right,new Vector3Int(0,0,1)}){
   overworld.blocks.Clear();var bottom=new Vector3Int(-17,65,-1);Frame(overworld,bottom,axis,false);
   for(int x=0;x<2;x++)for(int y=0;y<3;y++)Check(PortalRules.FindFrame(p=>overworld.GetBlock(p).type,bottom+axis*x+Vector3Int.up*y,false,out var f)&&f.bottom==bottom,"Frame from every interior cell");
   Check(!PortalRules.FindFrame(p=>overworld.GetBlock(p).type,bottom,true,out _),"Unlit frame cannot teleport");
   Frame(overworld,bottom,axis,true);Check(PortalRules.FindFrame(p=>overworld.GetBlock(p).type,bottom,true,out _),"Both portal axes");
   overworld.blocks[bottom+axis*2+Vector3Int.up].type=BlockType.Air;Check(!PortalRules.FindFrame(p=>overworld.GetBlock(p).type,bottom,true,out _),"Broken side invalidates portal");
  }
  GameManager.Instance=new GameManager();World.ActivateDimension(nether);Check(World.Instance==nether&&!overworld.worldRenderer.gameObject.active&&nether.worldRenderer.gameObject.active,"Only selected dimension renders");
  Check(GameManager.Instance.world==nether,"Streamer follows local dimension");
  overworld.blocks.Clear();nether.blocks.Clear();var portal=new Vector3Int(0,70,0);Frame(nether,portal,Vector3Int.right,true);
  nether.worldData.chunkDataDict[Vector3Int.zero]=new ChunkData{worldRef=nether,worldPos=Vector3Int.zero};
  var exit=nether.FindOrCreateExitAsync(new Vector3(0,70,0)).GetAwaiter().GetResult();Check(exit.y==70&&nether.GetBlock(Vector3Int.FloorToInt(exit)).type==BlockType.NetherPortal,"Reuse safe existing portal");
  var player=new GameObject("Player").AddComponent<Player>();player.ForTestAssignWorld(overworld);GameManager.Instance.players.Add(player);World.ActivateDimension(overworld);
  var sync=player.gameObject.AddComponent<Mirror.NetworkTransform>();
  nether.generationBarrier=new TaskCompletionSource<bool>();Call(player,"BeginDimensionTravel",WorldDimension.Nether,new Vector3(0,70,0),true);
  Check(player.DimensionTransferInProgress&&WorldLoadingScreen.IsLoading,"Frozen behind loading while destination generates");Check(player.dimension==WorldDimension.Overworld,"Source realm retained during generation");Check(!sync.enabled,"Old transform snapshots pause during travel");
  nether.generationBarrier.SetResult(true);Wait(()=>!player.DimensionTransferInProgress);
  Check(player.dimension==WorldDimension.Nether&&player.CurrentWorld==nether&&World.Instance==nether,"Completed travel updates player and camera world");
  Check(sync.enabled&&sync.resets>0,"Transform interpolation resets on arrival");
  Check(player.transform.position.y==70&&overworld.saves>0,"Source saved before travel");
  for(int i=0;i<100;i++)Call(player,"TickDimension");Check(!player.DimensionTransferInProgress&&player.dimension==WorldDimension.Nether,"Cooldown prevents portal bounce");
  // A deferred chunk from the old dimension stays in that dimension's dictionary.
  var before=overworld.worldData.chunkDataDict.Count;var data=new ChunkSaveData{position=new Vector3Int(-16,0,0)};
  nether.ApplyDimensionSnapshotsAsync(new Dictionary<Vector3Int,string>{{data.position,JsonUtility.ToJson(data)}}).GetAwaiter().GetResult();
  Check(nether.worldData.chunkDataDict.ContainsKey(data.position)&&overworld.worldData.chunkDataDict.Count==before,"Snapshot stays in destination dimension");
  Check(nether.worldData.chunkDataDict[data.position].worldRef==nether,"Deserialized chunk owns its dimension");
  var netSource=Realm(WorldDimension.Overworld);var netDestination=Realm(WorldDimension.Nether);World.Instance=netSource;
  var hosted=new GameObject("Host Player").AddComponent<Player>();hosted.ForTestAssignWorld(netSource);hosted.networkStarted=true;
  hosted.connectionToClient=new Mirror.NetworkConnectionToClient{identity=hosted.gameObject.AddComponent<Mirror.NetworkIdentity>()};
  Mirror.NetworkServer.active=true;
  Call(hosted,"BeginDimensionTravel",WorldDimension.Nether,new Vector3(2,75,2),false);
  Wait(()=>!hosted.DimensionTransferInProgress);
  Check(netSource.playerSaves>0&&netDestination.playerSaves>0,"Travel checkpoints source inventory before saving destination");
  Check(hosted.inventory.loads>=2,"Inventory reconciled at checkpoint and arrival");
  // Initial host joins reuse already prepared server terrain. They must not
  // checkpoint/save a source realm or serialize snapshots discarded by a host.
  var joiningRealm=Realm(WorldDimension.Overworld);World.Instance=joiningRealm;
  var readyChunk=new ChunkData{worldRef=joiningRealm,worldPos=Vector3Int.zero,betaPopulationDone=true,modifiedAfterSave=true};
  joiningRealm.worldData.chunkDataDict[Vector3Int.zero]=readyChunk;
  var joining=new GameObject("Initial Host").AddComponent<Player>();joining.ForTestAssignWorld(joiningRealm);joining.networkStarted=true;
  joining.connectionToClient=new Mirror.LocalConnectionToClient{identity=joining.gameObject.AddComponent<Mirror.NetworkIdentity>()};
  int snapshots=ChunkSaveData.serializations;
  joining.PrepareDimensionJoin();joining.SynchronizeDimensionOnJoin();Wait(()=>!joining.DimensionTransferInProgress);
  Check(joiningRealm.saves==0&&joiningRealm.worldSaveCalls==0,"Initial join performs no full departure/arrival world saves");
  Check(joiningRealm.generationCalls==0,"Initial host join does not regenerate prepared terrain");
  Check(ChunkSaveData.serializations==snapshots&&readyChunk.modifiedAfterSave,"Host skips discarded snapshots and retains unsaved dirty terrain");
  Check(joiningRealm.playerSaves==1&&joining.inventory.loads==1,"Initial join persists the reconciled player once");
  Check(!joiningRealm.HasReadyArea(Vector3Int.zero),"Readiness rejects absent renderers");
  var readyRenderer=new GameObject("Ready chunk").AddComponent<ChunkRenderer>();readyRenderer.Initialize(readyChunk);
  joiningRealm.worldData.chunkDict[Vector3Int.zero]=readyRenderer;
  Check(!joiningRealm.HasReadyArea(Vector3Int.zero),"Readiness requires raw-data halo");
  joiningRealm.worldData.chunkDataDict[new Vector3Int(16,0,0)]=new ChunkData();
  Check(joiningRealm.HasReadyArea(Vector3Int.zero),"Populated rendered chunk with halo is ready");
  readyChunk.betaPopulationDone=false;Check(!joiningRealm.HasReadyArea(Vector3Int.zero),"Readiness does not bypass deferred population");readyChunk.betaPopulationDone=true;
  readyRenderer.Initialize(new ChunkData());Check(!joiningRealm.HasReadyArea(Vector3Int.zero),"Readiness rejects stale pooled renderer data");readyRenderer.Initialize(readyChunk);
  joiningRealm.EnsureProductionAsync(Vector3Int.zero,true).GetAwaiter().GetResult();
  Check(joiningRealm.productionCoreCalls==0&&!joiningRealm.GenerationBusy,"Ready-area fast path skips core work and releases gate");
  readyChunk.betaPopulationDone=false;joiningRealm.EnsureProductionAsync(Vector3Int.zero,true).GetAwaiter().GetResult();
  Check(joiningRealm.productionCoreCalls==1&&joiningRealm.observedFullArea&&!joiningRealm.PreparingDimensionArea&&!joiningRealm.GenerationBusy,"Missing population still generates with full-area flag and releases gate");
  Mirror.NetworkServer.active=false;
  var populated=new HashSet<BlockType>();
  for(int seed=0;seed<30;seed++)
  {
   var a=Realm(WorldDimension.Overworld);a.betaWorldSeed=seed;a.baseBlock=p=>p.y<50||p.y>=90?BlockType.Netherrack:BlockType.Air;
   var b=Realm(WorldDimension.Nether);b.betaWorldSeed=seed;b.baseBlock=a.baseBlock;
   Beta173NetherPopulation.Populate(new ChunkData{worldRef=a},0,0);
   Beta173NetherPopulation.Populate(new ChunkData{worldRef=b},0,0);
   Check(a.blocks.Count==b.blocks.Count,"Stable Nether decoration count");
   foreach(var pair in a.blocks){Check(b.blocks.ContainsKey(pair.Key)&&b.blocks[pair.Key].type==pair.Value.type,"Stable Nether decoration positions");populated.Add(pair.Value.type);}
  }
  Check(populated.Contains(BlockType.GlowStone)&&populated.Contains(BlockType.Fire)&&populated.Contains(BlockType.BrownMushroom)&&populated.Contains(BlockType.RedMushroom),"Nether decoration passes place their blocks");
  var validState=new WorldServer.SavePlayerMessage{inventory=new PlayerInventory.Slot[36]};
  var validator=typeof(Player).GetMethod("ValidDimensionInventory",BindingFlags.NonPublic|BindingFlags.Static);
  Check((bool)validator.Invoke(null,new object[]{validState}),"Empty saved inventory accepted");
  validState.inventory[0]=new PlayerInventory.Slot{type=ItemType.Stone,count=65};
  Check(!(bool)validator.Invoke(null,new object[]{validState}),"Overfull arrival stacks rejected");
  validState.inventory[0].count=64;
  Check((bool)validator.Invoke(null,new object[]{validState}),"Bounded arrival inventory accepted");
  var factorySource=Realm(WorldDimension.Overworld);factorySource.worldName="NetherChecks-"+Guid.NewGuid().ToString("N");factorySource.isSaving=true;
  var made=World.GetDimension(WorldDimension.Nether);
  Check(made!=factorySource&&made.dimension==WorldDimension.Nether&&!made.spawnPassiveMobs,"Lazy dimension factory uses separate world and disables Nether animals");
  GameManager.Instance.localPlayer=player;World.ActivateDimension(made);int restarts=GameManager.Instance.streamRestarts;made.OnNewChunksGenerated.Invoke();Check(GameManager.Instance.streamRestarts==restarts+1,"Nether streaming restarts after additional chunks finish");
  Check(!made.isSaving&&made.worldData.owner==made,"Factory does not inherit runtime save state or chunk ownership");
  Check(made.StorageName==factorySource.worldName+"/DIM-1"&&World.GetDimension(WorldDimension.Nether)==made,"Factory cache retains shared save root");
  Check(Debug.errors.Count==0,"No asynchronous transfer failures");
  Console.WriteLine(checks+" dimension/portal/transfer/save isolation checks passed (Unity/Mirror mocked).");
 }
}
