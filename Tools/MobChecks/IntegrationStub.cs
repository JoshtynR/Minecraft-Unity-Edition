using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager {public static GameManager Instance;public Player localPlayer;}
public static class WorldDataHelper {public static Vector3Int GetChunkPosition(World world,Vector3Int p)=>new Vector3Int((int)Math.Floor(p.x/16f)*16,(int)Math.Floor(p.y/16f)*16,(int)Math.Floor(p.z/16f)*16);}
public static class BlockShapesExtensions {public static bool IsPlant(BlockType type)=>false;}
public static class BlockSoundRules {public static string Step(BlockType type)=>"step/grass";}
public struct TestDirection {public Vector3Int vector;public Vector3Int GetVector()=>vector;}
public static class BlockHelper {public static TestDirection[] directions={new TestDirection{vector=new Vector3Int(0,1,0)}};}
public static class ItemDurability {public static int HitWear(ItemType type)=>1;public static bool Wear(ref InventoryStack stack,int count){stack.damage+=count;return false;}}
public static class ChestTransactions {public static bool Valid(InventoryStack[] snapshot,InventoryStack cursor)=>snapshot!=null&&snapshot.Length==36;}
public static class ItemVisualMesh {public static Mesh Build(BlockType type)=>new Mesh{name="Fire geometry double"};}
public partial class World {
 public static World Instance;public int chunkSize=16;public long betaWorldSeed=173;public bool IsWorldCreated=true,DropAuthority=true,isSaving,disabled,GenerationBusy;
 private int betaPopulationJobs;
 public void PopulationBusyForCheck(bool busy){betaPopulationJobs=busy?1:0;}
 public World(){Instance=this;for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)for(int y=0;y<8;y++)worldData.chunkDict[new Vector3Int(x*16,y*16,z*16)]=1;spawnPassiveMobs=spawnHostileMobs=false;EnsureMobsRestored();}
 public static World FindDimension(WorldDimension dimension)=>Instance;
 public void PhysicsStep(MobState mob){mobTick++;mobNavigationBudget=2;activeMobs.Clear();activeMobs.AddRange(mobs.Entities);TickMob(mob);}
 public void RenderForCheck()=>UpdateMobVisuals();public void ClearForCheck()=>ResetMobs(true);public void RegisterForCheck()=>InitializeMobs();
 public void FrameForCheck(float dt){Time.deltaTime=dt;UpdateMobs();}
 public void RestoreForCheck(MobSaveData data){RestoreMobs(data);EnsureMobsRestored();}
 public int SpawnHostilesForCheck(){spawnHostileMobs=true;skyLightReduction=11;for(int i=0;i<40&&mobs.Count==0;i++)SpawnNaturalHostiles();return mobs.Count;}
 public int RendererCount=>mobVisuals.Count;
 public Vector3 VisualPositionForCheck(int id)=>mobVisuals[id].root.transform.position;
 public bool SpecificSpawnForCheck(MobType type,Vector3 position)=>BetaSpecificSpawn(type,position);
 public bool SpawnClearForCheck(MobType type,Vector3 position)=>MobSpawnClear(position,type);
}
