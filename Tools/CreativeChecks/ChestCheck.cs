using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Mirror;

namespace UnityEngine
{
 // The serializer boundary is mocked; production uses Unity JsonUtility JSON.
 public static class JsonUtility
 {
  public static string ToJson(object value){using(var stream=new MemoryStream()){new BinaryFormatter().Serialize(stream,value);return Convert.ToBase64String(stream.ToArray());}}
  public static T FromJson<T>(string text){using(var stream=new MemoryStream(Convert.FromBase64String(text)))return (T)new BinaryFormatter().Deserialize(stream);}
 }
}
namespace UnityEngine.Rendering {public enum IndexFormat {UInt32}public enum ShadowCastingMode {Off}}
public partial class World:MonoBehaviour
{
 public bool GenerationBusy;
 public World worldRenderer=>this;public WorldDimension dimension;public string StorageName=>DimensionRules.StorageName(worldName,dimension);public static World FindDimension(WorldDimension id)=>Instance;public static System.Collections.Generic.IEnumerable<World> LoadedDimensions=>new[]{Instance};
 public bool IsWorldCreated=true,disabled;public int worldHeight=128;
 public static World Instance=new World();public string worldName="";public int skyLightReduction;public bool isSaving;public void SaveWorld(){}
 public System.Collections.Generic.Dictionary<Vector3Int,Block> blocks=new System.Collections.Generic.Dictionary<Vector3Int,Block>();
 public World(){InitializeChests();}
 public void AddChunkToUpdate(ChunkRenderer renderer){}
 public Block GetBlock(Vector3Int position){Block block;return blocks.TryGetValue(position,out block)?block:new Block(BlockType.Air,position,null);}
}
public partial class WorldServer {public static WorldServer instance;public bool isServer=true;public void PollChests(){Update();}}
public static class DirectionExtensions
{
 public static Vector3Int GetVector(this Direction direction)=>direction==Direction.up?new Vector3Int(0,1,0):direction==Direction.down?new Vector3Int(0,-1,0):direction==Direction.left?new Vector3Int(-1,0,0):direction==Direction.right?new Vector3Int(1,0,0):direction==Direction.backwards?new Vector3Int(0,0,-1):new Vector3Int(0,0,1);
}
public partial class PlayerInventory
{
 static int ChestTotal(ChestReply reply)=>reply.chest.Sum(x=>x.count)+reply.inventory.Sum(x=>x.count)+reply.cursor.count;
 static void CheckChestFaceUV(int part,Direction face,params int[] pixels)
 {
  var data=ChestModelGeometry.Build(part);int start=Array.IndexOf(BlockHelper.directions,face)*4;
  for(int i=0;i<4;i++)
   Check(Math.Abs(data.uv[start+i].x*64-pixels[i*2])<.0001f&&Math.Abs((1-data.uv[start+i].y)*64-pixels[i*2+1])<.0001f,
    "entity atlas pixel corner follows native box face order: part "+part+" "+face+" corner "+i);
 }
 static void CheckChests(PlayerInventory menu)
 {
  // Explicit native box atlas corners, in BlockShape vertex order. In particular,
  // the body's dark interior belongs on top, while its wood base faces down.
  CheckChestFaceUV(0,Direction.up,28,19,42,19,42,33,28,33);
  CheckChestFaceUV(0,Direction.down,14,33,28,33,28,19,14,19);
  CheckChestFaceUV(0,Direction.backwards,42,33,42,43,56,43,56,33);
  CheckChestFaceUV(0,Direction.forwards,14,33,14,43,28,43,28,33);
  CheckChestFaceUV(0,Direction.left,0,33,0,43,14,43,14,33);
  CheckChestFaceUV(0,Direction.right,28,33,28,43,42,43,42,33);
  CheckChestFaceUV(1,Direction.up,28,0,42,0,42,14,28,14);
  CheckChestFaceUV(1,Direction.down,14,14,28,14,28,0,14,0);
  CheckChestFaceUV(1,Direction.backwards,42,14,42,19,56,19,56,14);
  CheckChestFaceUV(1,Direction.forwards,14,14,14,19,28,19,28,14);
  CheckChestFaceUV(2,Direction.backwards,4,1,4,5,6,5,6,1);
  CheckChestFaceUV(2,Direction.forwards,1,1,1,5,3,5,3,1);
  for(int part=0;part<3;part++)
  {
   var mesh=ChestModelGeometry.Build(part);
   Check(mesh.vertices.Count==24&&mesh.triangles.Count==36&&mesh.uv.Count==24,"modern body/lid/latch have six textured faces");
   foreach(var uv in mesh.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"modern chest unwrap stays inside 64x64 atlas");
  }
  Check(ChestModelGeometry.LidAngle(0)==0&&ChestModelGeometry.LidAngle(1)==90&&ChestModelGeometry.LidAngle(.5f)>45,"hinged lid uses eased quarter-turn");
  Check(ChestModelGeometry.Facing(2)==0&&ChestModelGeometry.Facing(3)==180&&ChestModelGeometry.Facing(4)==90&&ChestModelGeometry.Facing(5)==-90,"all four block directions rotate modern chest consistently");
  var random=new System.Random(42);
  for(int run=0;run<200;run++)
  {
   var chest=new InventoryStack[27];var inventory=new InventoryStack[36];var cursor=new InventoryStack();
   for(int i=0;i<27;i++)chest[i]=new InventoryStack(ItemType.Stone,random.Next(65));
   for(int i=0;i<36;i++)inventory[i]=new InventoryStack(ItemType.Dirt,random.Next(65));
   int total=chest.Sum(x=>x.count)+inventory.Sum(x=>x.count);
   for(int click=0;click<100;click++)
   {
    Check(ChestTransactions.Click(chest,inventory,ref cursor,random.Next(63),random.Next(2)==0,random.Next(3)==0),"valid chest transaction accepted");
    Check(chest.Sum(x=>x.count)+inventory.Sum(x=>x.count)+cursor.count==total,"chest clicks conserve all items");
    Check(chest.Concat(inventory).All(x=>x.count>=0&&x.count<=64),"chest transactions respect stack bounds");
   }
  }
  var store=new ChestStorage();int opens=0,closes=0;store.LidChanged+=(p,o)=>{if(o)opens++;else closes++;};
  var pos=new Vector3Int(2,3,4);var source=new InventoryStack[36];source[0]=new InventoryStack(ItemType.Stone,63);
  World.Instance.blocks[pos]=new Block(BlockType.Chest,pos,null);
  var open=new ChestRequest{action=0,position=pos,inventory=source};
  var reply=store.Process("",1,open,true);Check(reply.accepted&&reply.opened&&opens==1,"open creates a server-owned chest session and opens lid");
  source[0].count=1;
  Check(!store.Process("",2,open,true).accepted,"second player cannot race an active chest session");
  reply=store.Process("",1,new ChestRequest{action=1,position=pos,sequence=1,slot=27,shift=true},true);
  Check(reply.chest[0].count==63&&reply.inventory[0].Empty&&ChestTotal(reply)==63,"server transfer uses original session snapshot rather than later client array edits");
  Check(!store.Process("",1,new ChestRequest{action=1,position=pos,sequence=1,slot=0},true).accepted,"replayed sequence cannot duplicate a transfer");
  reply=store.Process("",1,new ChestRequest{action=1,position=pos,sequence=2,slot=0},true);
  reply=store.Process("",1,new ChestRequest{action=4,position=pos,sequence=3,slots=new[]{27,28,29},right=true},true);
  Check(reply.inventory.Take(3).All(x=>x.count==1)&&reply.cursor.count==60,"server-authoritative chest drag paints across player slots");
  reply=store.Process("",1,new ChestRequest{action=5,position=pos,sequence=4,slot=27},true);
  Check(reply.cursor.count==63&&reply.inventory.Take(3).All(x=>x.Empty),"chest double click consolidates matching loose stacks onto cursor");
  reply=store.Process("",1,new ChestRequest{action=6,position=pos,sequence=5,right=true},true);
  Check(reply.dropped.count==1&&reply.cursor.count==62,"chest outside right click returns one authoritative world-drop stack");
  reply.chest[0].count=0;
  Check(!store.CanRemove("",pos),"occupied chest cannot be broken");
  reply=store.Close(1);Check(!reply.opened&&reply.chest[0].Empty&&closes==1&&store.CanRemove("",pos),"closing retains the updated server contents and shuts lid");
  Check(!store.Process("",1,open,false).accepted,"out-of-range or obstructed open rejected");
  open.inventory=new InventoryStack[35];Check(!store.Process("",1,open,true).accepted,"malformed inventory rejected");
  string name="chest-check-"+Guid.NewGuid().ToString("N");
  string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves",name);
  try
  {
   store=new ChestStorage();open.inventory=new InventoryStack[36];open.inventory[0]=new InventoryStack(ItemType.Stick,19);
   store.Process(name,1,open,true);store.Process(name,1,new ChestRequest{action=1,position=pos,sequence=1,slot=27,shift=true},true);store.Close(1);store.SaveWorld(name);store.SaveWorld(name);
   Check(File.Exists(Path.Combine(directory,"chests.json.bak")),"save keeps previous chest file as backup");
   store=new ChestStorage();reply=store.Process(name,7,open,true);
   Check(reply.chest[0].count==19&&reply.chest[0].type==ItemType.Stick,"contents survive storage reconstruction and disk reload");
  }
  finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
  menu.LoadInventory(null);menu.AddItem(ItemType.Stone,63);menu.RequestChest(pos);
  Check(menu.chestOpen&&menu.isOpen&&menu.chestPanel!=null&&!menu.creativePanel.gameObject.activeSelf,"right-click bridge opens 27-slot container without creative catalogue");
  Check(menu.inventorySlotsRect.parent==menu.chestPanel&&menu.chestSlots.Length==27,"container reuses all 36 player storage slots");
  Check(menu.chestPanel.anchoredPosition.x==0,"chest opens centered without catalogue space");
  CheckContainerLabel(menu,menu.chestPanel,"ChestTitle",8,6);
  CheckContainerLabel(menu,menu.chestPanel,"PlayerTitle",8,74);
  menu.ClickInventorySlot(0,false,true);
  Check(menu.chestSlots[0].count==63&&menu.slots[0].count==0,"shift-click player stack into chest");
  menu.ClickInventorySlot(100,true,false);
  Check(menu.pickedUpSlot.count==32&&menu.chestSlots[0].count==31,"right-click splits chest stack");
  menu.CloseInventory();Check(!menu.chestOpen&&!menu.isOpen&&menu.slots.Sum(s=>s.count)==32,"close returns carried stack safely to player");
  menu.OpenInventory();Check(!menu.creativePanel.gameObject.activeSelf&&menu.inventoryRect.anchoredPosition.x==0,"closing chest preserves disabled TMI setting");menu.CloseInventory();
  menu.ToggleTooManyItems();
  menu.RequestChest(pos);Check(menu.chestSlots[0].count==31,"reopening retains contents");
  Check(menu.tooManyItemsEnabled&&!menu.creativePanel.gameObject.activeSelf&&menu.chestPanel.anchoredPosition.x==0,"chest stays centered while remembering TMI preference");
  menu.ClickInventorySlot(100,false,true);menu.CloseInventory();
  menu.OpenInventory();Check(menu.creativePanel.gameObject.activeSelf,"normal inventory restores enabled TMI preference after chest");menu.CloseInventory();menu.ToggleTooManyItems();
  Check(menu.slots.Sum(s=>s.count)==63,"round trip returns exactly original item count");
  NetworkClient.active=true;
  menu.RequestChest(pos);var networkStore=new ChestStorage();
  var opened=networkStore.Process("",99,NetworkClient.last,true);menu.ReceiveChestReply(opened);
  menu.ClickInventorySlot(0,false,true);var clickRequest=NetworkClient.last;
  menu.CloseInventory();Check(menu.chestPending&&menu.chestCloseRequested&&menu.isOpen,"close waits for in-flight transfer reply");
  menu.ReceiveChestReply(networkStore.Process("",99,clickRequest,true));
  Check(NetworkClient.last.action==2&&menu.chestPending,"queued close is sent after acknowledged transfer");
  menu.ReceiveChestReply(networkStore.Process("",99,NetworkClient.last,true));
  Check(!menu.chestOpen&&!menu.chestPending&&!menu.isOpen,"close acknowledgement restores normal inventory");
  NetworkClient.active=false;
  var eye=new Vector3(2.5f,3.5f,2);
  Check(World.Instance.ChestAccessible(eye,pos,4.5f),"actual world access accepts a visible nearby chest");
  World.Instance.blocks[pos+Vector3Int.up]=new Block(BlockType.Stone,pos+Vector3Int.up,null);
  Check(!World.Instance.ChestAccessible(eye,pos,4.5f),"solid block above prevents opening lid");World.Instance.blocks.Remove(pos+Vector3Int.up);
  var obstruction=new Vector3Int(2,3,3);World.Instance.blocks[obstruction]=new Block(BlockType.Stone,obstruction,null);
  Check(!World.Instance.ChestAccessible(eye,pos,4.5f),"server rejects interaction through opaque wall");World.Instance.blocks.Remove(obstruction);
  Check(!World.Instance.ChestAccessible(new Vector3(20,3,4),pos,4.5f),"server rejects distant interaction");
  var server=new WorldServer();WorldServer.instance=server;NetworkServer.active=true;
  var connection=new NetworkConnectionToClient{connectionId=77,identity=menu.gameObject};NetworkServer.connections[77]=connection;
  server.ReceiveChestRequest(connection,new ChestRequest{action=0,position=pos,inventory=new InventoryStack[36]});
  Check(connection.last.accepted&&World.Instance.openChestLids.Contains(pos),"actual server handler opens chest and broadcasts lid state");
  World.Instance.isSaving=true;server.ReceiveChestRequest(connection,new ChestRequest{action=1,position=pos,sequence=1,slot=0});
  Check(!connection.last.accepted,"server pauses chest transfers while world snapshot is being saved");World.Instance.isSaving=false;
  Check(!World.Instance.CanReplaceChest(pos,BlockType.Air),"even an empty chest is protected while being used");
  menu.player.health=0;Time.unscaledTime+=.5f;server.PollChests();
  Check(!connection.last.opened&&!World.Instance.openChestLids.Contains(pos)&&World.Instance.chests.Owners().Length==0,"death releases server session and shuts lid");
  menu.player.health=20;NetworkServer.active=false;NetworkServer.connections.Clear();WorldServer.instance=null;
  Resources.hud=new PlayerHudAssets{chestSkin=new Texture2D{width=64,height=64}};ModernChestVisual.Prepare();
  Check(ModernChestVisual.Enabled,"assigned sheet enables modern entity renderer");
  var model=new GameObject("ChestVisual").AddComponent<ModernChestVisual>();model.Initialize(World.Instance,pos,5);
  var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
  var tick=typeof(ModernChestVisual).GetMethod("Update",flags);
  int soundCount=GameAudio.plays;
  World.Instance.SetChestLid(pos,true);World.Instance.SetChestLid(pos,true);
  Check(GameAudio.plays==soundCount+1,"repeated lid replication does not duplicate chest-open sound");
  for(int i=0;i<10;i++)tick.Invoke(model,null);
  Check((float)typeof(ModernChestVisual).GetField("progress",flags).GetValue(model)==1,"rendered lid reaches open state");
  World.Instance.SetChestLid(pos,false);for(int i=0;i<10;i++)tick.Invoke(model,null);
  Check(GameAudio.plays==soundCount+2,"closing chest emits one additional sound");
  Check((float)typeof(ModernChestVisual).GetField("progress",flags).GetValue(model)==0,"rendered lid returns to closed state");
  var owned=(System.Collections.Generic.List<Mesh>)typeof(ModernChestVisual).GetField("meshes",flags).GetValue(model);
  var material=(Material)typeof(ModernChestVisual).GetField("material",flags).GetValue(model);
  typeof(ModernChestVisual).GetMethod("OnDestroy",flags).Invoke(model,null);
  Check(owned.All(m=>m.destroyed)&&material.destroyed&&!Resources.hud.chestSkin.destroyed,"chest renderer releases owned native meshes and material, preserving source skin");
  var chunkObject=new GameObject("Chunk",typeof(ChunkRenderer));var renderer=chunkObject.GetComponent<ChunkRenderer>();
  var chunk=new ChunkData{worldRef=World.Instance};renderer.Initialize(chunk);
  Func<MeshData> freshUpload=()=>new MeshData(true){chests=new System.Collections.Generic.List<Vector3Int>{pos},sourceChunk=chunk};
  renderer.RenderMesh(freshUpload());Check(chunkObject.transform.childCount==1,"actual chunk mesh upload creates a separate chest model");
  var child=chunkObject.transform.GetChild(0).gameObject;renderer.RenderMesh(freshUpload());
  Check(chunkObject.transform.childCount==1&&ReferenceEquals(child,chunkObject.transform.GetChild(0).gameObject),"lighting remesh reuses chest model rather than duplicating owned resources");
  World.Instance.blocks.Remove(pos);renderer.RenderMesh(freshUpload());
  Check(chunkObject.transform.childCount==0&&child.destroyed,"stale chest mesh upload cannot resurrect mined chest model");
  Resources.hud=null;
  menu.LoadInventory(null);
 }
}
