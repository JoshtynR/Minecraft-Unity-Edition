using System;using System.Collections.Generic;using System.Linq;using System.Threading;using System.Threading.Tasks;
namespace UnityEngine {
 public class Object { public string name;public static void Destroy(Object o){} }
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool inactive=false)where T:Component=>gameObject.GetComponentsInChildren<T>();}
 public class MonoBehaviour:Component {}
 public class GameObject:Object {
  public bool active=true;public Transform transform;public List<Component> components=new List<Component>();
  public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};components.Add(transform);}
  public void SetActive(bool value){bool before=active;active=value;if(!before&&value)foreach(var c in components)c.GetType().GetMethod("Awake")?.Invoke(c,null);}
  public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>()where T:Component=>components.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();
 }
 public class Transform:Component {public Vector3 position;public List<Transform> children=new List<Transform>();public void SetParent(Transform parent,bool stays){parent.children.Add(this);}}
 public class Canvas:Component {public bool enabled=true;}
 public class Renderer:Component {public bool enabled=true;}
 public class Collider:Component {public bool enabled=true;}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>this*(1/Math.Max(.00001f,magnitude));
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;
 }
 public struct Vector3Int {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int zero=>new Vector3Int();public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);public static Vector3Int left=>new Vector3Int(-1,0,0);public static Vector3Int right=>new Vector3Int(1,0,0);
  public static Vector3Int RoundToInt(Vector3 p)=>FloorToInt(p);
  public static Vector3Int FloorToInt(Vector3 p)=>new Vector3Int((int)Math.Floor(p.x),(int)Math.Floor(p.y),(int)Math.Floor(p.z));
  public static implicit operator Vector3(Vector3Int a)=>new Vector3(a.x,a.y,a.z);public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3Int operator-(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3Int operator*(Vector3Int a,int s)=>new Vector3Int(a.x*s,a.y*s,a.z*s);
  public static bool operator==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator!=(Vector3Int a,Vector3Int b)=>!(a==b);public override bool Equals(object o)=>o is Vector3Int&&(Vector3Int)o==this;public override int GetHashCode()=>x*397^y*31^z;
 }
 public static class Mathf {public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int FloorToInt(float v)=>(int)Math.Floor(v);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Clamp01(float v)=>Clamp(v,0,1);}
 public static class Time {public static float unscaledTime,time,fixedDeltaTime=.05f;}
 public static class Debug {public static List<Exception> errors=new List<Exception>();public static void LogException(Exception e){errors.Add(e);}public static void Log(object message){}}
 public static class JsonUtility {static Dictionary<string,object> objects=new Dictionary<string,object>();public static string ToJson(object obj){string key=Guid.NewGuid().ToString();objects[key]=obj;return key;}public static T FromJson<T>(string s)=>(T)objects[s];public static void FromJsonOverwrite(string s,object obj){foreach(var field in obj.GetType().GetFields())if(!field.IsInitOnly&&!field.IsStatic)field.SetValue(obj,field.GetValue(objects[s]));}}
}
namespace UnityEngine.Events {public class UnityEvent {private event Action listeners;public void AddListener(Action action){listeners+=action;}public void Invoke(){listeners?.Invoke();}}}
namespace Mirror {
 public class NetworkTransformBase:UnityEngine.Component {public bool enabled=true;public int resets;public void Reset(){resets++;}}
 public class NetworkTransform:NetworkTransformBase {public void RpcTeleport(UnityEngine.Vector3 p){transform.position=p;Reset();}}
 public class SyncVarAttribute:Attribute {public string hook;}
 public class CommandAttribute:Attribute {}
 public class TargetRpcAttribute:Attribute {}
 public class NetworkConnection {public NetworkIdentity identity;}
 public class NetworkConnectionToClient:NetworkConnection {public int connectionId;public bool disconnected;public List<object> messages=new List<object>();public void Send<T>(T m){messages.Add(m);}public void Disconnect(){disconnected=true;}}
 public class LocalConnectionToClient:NetworkConnectionToClient {}
 public class NetworkIdentity:UnityEngine.Component {public void AssignClientAuthority(NetworkConnectionToClient c){}}
 public static class NetworkServer {public static bool active;
 public static void RegisterHandler<T>(Action<NetworkConnectionToClient,T> handler,bool auth=true){}
 public static void AddPlayerForConnection(NetworkConnectionToClient c,UnityEngine.GameObject player){c.identity=player.AddComponent<NetworkIdentity>();}
 }
}
public struct WorldData {public string worldName;public World owner;public Dictionary<UnityEngine.Vector3Int,ChunkData> chunkDataDict;public Dictionary<UnityEngine.Vector3Int,ChunkRenderer> chunkDict;}
public class WorldRenderer:UnityEngine.MonoBehaviour {public UnityEngine.GameObject chunkPrefab;}
public class TerrainGenerator:UnityEngine.MonoBehaviour {public bool dumpBetaParity;}
public class Block {public UnityEngine.Vector3Int globalWorldPosition;public BlockType type;public byte blockShape;public ChunkData chunkData;}
public class ChunkData {public UnityEngine.Vector3Int worldPos;public bool modifiedByPlayer,modifiedAfterSave,HasPendingLighting,betaPopulationDone;public World worldRef;public Dictionary<UnityEngine.Vector3Int,Block> blocks=new Dictionary<UnityEngine.Vector3Int,Block>();public static ChunkData Deserialize(ChunkSaveData s,World w)=>new ChunkData{worldRef=w,worldPos=s.position};public void UpdateLight(){HasPendingLighting=false;}}
public class ChunkSaveData {public static int serializations;public UnityEngine.Vector3Int position;public static ChunkSaveData Serialize(ChunkData d){serializations++;return new ChunkSaveData{position=d.worldPos};}}
public class ChunkRenderer:UnityEngine.MonoBehaviour {public ChunkData ChunkData;public void Initialize(ChunkData d){ChunkData=d;}public void UpdateChunk(object mesh){}}
public class WorldSaveData {public object fluidTicks,droppedItems,cows;}
public struct DropStateMessage {public WorldDimension dimension;public object item;}
public class GameManager {public static GameManager Instance;public List<Player> players=new List<Player>();public World world;public Player localPlayer;public int streamRestarts;public void StartCheckingForChunks(){streamRestarts++;}}
public static class WorldLoadingScreen {public static bool IsLoading;public static void Begin(){IsLoading=true;}public static void Cancel(){IsLoading=false;}}
public static class GameAudio {public static void Play(string s,UnityEngine.Vector3 p,float v=1){}public static void Broadcast(string s,UnityEngine.Vector3 p,float v,float pitch,World world){}public static void BroadcastBlock(BlockType t,UnityEngine.Vector3Int p,bool broken,World world=null){}}
public static class SpecialBlockGeometry {public static bool NormalCube(BlockType b)=>b!=BlockType.Air&&b!=BlockType.Nothing&&b!=BlockType.NetherPortal&&b!=BlockType.Fire;public static bool NoCollision(BlockType b)=>!NormalCube(b);public static byte Shape(BlockType t,byte s)=>s;public static byte State(BlockType t,byte s)=>s;public static UnityEngine.Vector3Int PartnerOffset(BlockType t,byte s)=>UnityEngine.Vector3Int.right;}
public static class BlockShapesExtensions {public static bool IsPlant(BlockType t)=>t==BlockType.BrownMushroom||t==BlockType.RedMushroom;}
public static class WorldDataHelper {
 public static List<UnityEngine.Vector3Int> GetChunkPositionsInRenderDistance(World w,UnityEngine.Vector3Int p)=>new List<UnityEngine.Vector3Int>{GetChunkPosition(w,p)};
 public static List<UnityEngine.Vector3Int> GetDataPositionsInRenderDistance(World w,UnityEngine.Vector3Int p)=>new List<UnityEngine.Vector3Int>{GetChunkPosition(w,p),GetChunkPosition(w,p)+new UnityEngine.Vector3Int(16,0,0)};
 public static UnityEngine.Vector3Int GetChunkPosition(World w,UnityEngine.Vector3Int p)=>new UnityEngine.Vector3Int((int)Math.Floor(p.x/16.0)*16,0,(int)Math.Floor(p.z/16.0)*16);
 public static ChunkRenderer GetChunk(World w,UnityEngine.Vector3Int p){w.worldData.chunkDict.TryGetValue(p,out var c);return c;}
 public static void SetBlock(World w,UnityEngine.Vector3Int p,BlockType t){var block=w.GetBlock(p);block.type=t;w.blocks[p]=block;}
}
public partial class World:UnityEngine.MonoBehaviour {
 public static World Instance;public string worldName="test";public long betaWorldSeed;public bool spawnCows=true,DropAuthority=true,GenerateMoreChunks=true,isSaving,disabled,IsWorldCreated=true;public int worldHeight=128,renderDistance=1,chunkSize=16,chunksPerFrame=1;
 public WorldData worldData;public WorldRenderer worldRenderer;public TerrainGenerator terrainGenerator;public UnityEngine.Events.UnityEvent OnWorldCreated,OnNewChunksGenerated;
 private int lastBetaSkyDarkness,lastBetaBrightnessBoost;private readonly SemaphoreSlim generationGate=new SemaphoreSlim(1,1);private readonly CancellationToken worldCancellationToken=CancellationToken.None;private bool chunkUpdateInProgress,waterMeshBuildInProgress;
 public List<ChunkRenderer> chunksToUpdate=new List<ChunkRenderer>();public Queue<Block> blockToUpdate=new Queue<Block>();
 public ContainerStore chests=new ContainerStore(),furnaces=new ContainerStore();
 public TaskCompletionSource<bool> generationBarrier;public int generationCalls,saves,meshRebuilds;
 public Func<UnityEngine.Vector3Int,BlockType> baseBlock;
 public Dictionary<UnityEngine.Vector3Int,Block> blocks=new Dictionary<UnityEngine.Vector3Int,Block>();
 public void Awake(){RegisterDimension();}
 public Block GetBlock(UnityEngine.Vector3Int p){if(blocks.TryGetValue(p,out var b))return b;return new Block{type=baseBlock==null?BlockType.Air:baseBlock(p),chunkData=new ChunkData{worldRef=this}};}
 public Block GetBlock(UnityEngine.Vector3 p)=>GetBlock(UnityEngine.Vector3Int.FloorToInt(p));
 public Task EnsureAreaAsync(UnityEngine.Vector3Int p,bool full=false){generationCalls++;return generationBarrier==null?Task.CompletedTask:generationBarrier.Task;}
 public void GenerateWorld(UnityEngine.Vector3Int p){}
 public Task SaveDimensionAsync(){saves++;return Task.CompletedTask;}
 public int playerSaves;public void SavePlayer(object save){playerSaves++;}
 public int worldSaveCalls;public void SaveWorld(){worldSaveCalls++;}
 public Task EnsureWorldMetadataAsync()=>Task.CompletedTask;
 public static string GetChunkPath(string name,UnityEngine.Vector3Int p,WorldDimension dimension=WorldDimension.Overworld)=>"/nonexistent/test-chunk";
 public Task<Dictionary<UnityEngine.Vector3Int,ChunkData>> LoadChunksAsync(IEnumerable<UnityEngine.Vector3Int> positions,string name)=>Task.FromResult(new Dictionary<UnityEngine.Vector3Int,ChunkData>());
 public void UpadteLightTexture(){}
 public void RestoreFluidTicks(object x){}public void RestoreDroppedItems(object x){}public void RestoreCows(object x){}
 public void SendCowSnapshot(Mirror.NetworkConnectionToClient c){}
 public object[] CaptureDroppedItems()=>new object[0];
 public static string Compress(string s)=>s;public static string Decompress(string s)=>s;
 private void ResetChunkUpdates(){}
 public Task CastLightFirstTime(List<ChunkData> chunks)=>Task.CompletedTask;
 public Task<Dictionary<UnityEngine.Vector3Int,object>> CreateMeshDataAsync(List<ChunkData> chunks){meshRebuilds++;return Task.FromResult(new Dictionary<UnityEngine.Vector3Int,object>());}
 public void AddChunkToUpdate(ChunkRenderer c,bool first){}
 public bool CanReplaceChest(UnityEngine.Vector3Int p,BlockType t)=>true;public void ReceiveWaterState(UnityEngine.Vector3Int p,byte state){}
 public void SetBlock(UnityEngine.Vector3Int p,BlockType t,byte state=0){WorldDataHelper.SetBlock(this,p,t);QueuePortalValidation(p);}
 private void SetFarmBlock(UnityEngine.Vector3Int p,BlockType t){SetBlock(p,t);}
}
public partial class Player:UnityEngine.MonoBehaviour {
 protected World world;public bool isLocalPlayer=true,isServer=true,networkStarted;private bool loadingSavedPlayer;private int knownItemWearRevision;public bool AcceptsItemRevision(int revision)=>revision==0;public ulong SteamId;public bool expectsSavedPlayerData;public void RpcLoadPlayer(Mirror.NetworkConnection c,object data){}public int health=20;public PlayerInventory inventory=new PlayerInventory();public Mirror.NetworkConnectionToClient connectionToClient;
 private bool serverMining,localMining,hasFluidSafePosition,jumpRequest,survivalPositionKnown;private float verticalMomentum,localWalkDistance,localSprintDistance;private UnityEngine.Vector3 velocity,extraForce;
 public void ResetFluidMovement(){}public void ApplyFluidRespawn(UnityEngine.Vector3 p){health=20;}
 public WorldServer.SavePlayerMessage SavePlayer()=>new WorldServer.SavePlayerMessage{inventory=new PlayerInventory.Slot[36]};private bool InMiningReach(UnityEngine.Vector3Int p)=>true;
 public void ForTestAssignWorld(World w){world=w;dimension=w.dimension;}
}
public class PlayerInventory:UnityEngine.Component {
 public bool isOpen;public void CloseInventory(){isOpen=false;}public int loads;
 public void LoadInventory(Slot[] inventory,Slot[] crafting,Slot cursor,Slot[] recovery,Slot[] equipment){loads++;}
 public void ApplyChestPlayerSnapshot(object inventory,object cursor){}
 public class Slot {public ItemType type;public int count,damage;}}
public static class ItemCatalog {public static bool Valid(ItemType type)=>type!=ItemType.Nothing&&Enum.IsDefined(typeof(ItemType),type);public static int StackLimit(ItemType type)=>64;}
public class ContainerReply {public bool accepted;public object inventory,cursor;}
public class ContainerStore {public ContainerReply Close(int id)=>new ContainerReply();}

#if !CHECK_SERVER
public partial class WorldServer {public struct SavePlayerMessage {public PlayerInventory.Slot[] inventory,crafting,recovery,equipment;public PlayerInventory.Slot cursor;}public static WorldServer instance;public void CloseChestForDisconnect(Mirror.NetworkConnectionToClient c){}public void CloseFurnaceForDisconnect(Mirror.NetworkConnectionToClient c){}public void BroadcastBlock(World w,UnityEngine.Vector3Int p,BlockType t,byte s){}}

#endif
