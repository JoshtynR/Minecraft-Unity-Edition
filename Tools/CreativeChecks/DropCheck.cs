using System;
using System.Linq;
using UnityEngine;
namespace Mirror {public static partial class NetworkClient {public static WorldServer.SetBlockMessage placement;public static void Send(WorldServer.SetBlockMessage value){placement=value;}}}
public partial class WorldServer {public struct SetBlockMessage {public int requestId;public SetBlockMessage(Vector3Int p,BlockType t,byte state,int request){requestId=request;}}}

// Fluid/collision dependencies have their own production parity suites. Here the
// ground probe supplies collision results to exercise the real drop update loop.
public static class BlockCollision
{
    public static Vector3 Resolve(World world,Vector3 feet,float radius,float height,Vector3 move,bool step)
    {if(feet.y+move.y<0)move.y=-feet.y;return move;}
    public static bool TouchesBlock(World world,Vector3 feet,float radius,float height,BlockType type)
    {return world.GetBlock(Vector3Int.FloorToInt(feet)).type==type;}
    public static bool Supported(World world,Vector3 feet,float radius,float height)=>feet.y<=0;
}
public static class FluidWorldQueries
{
    public static int Sample(World world,WaterPosition p,BlockType type)=>-1;
    public static bool SolidFace(World world,WaterPosition p)=>false;
}

public partial class World
{
    public int betaPopulationJobs;
    public int DropVisualCount=>dropVisuals.Count;public Mesh FirstDropMesh=>dropMeshes.Values.First();public Material FirstDropMaterial=>dropMaterials.Values.First();
    public void StepDrops(float dt){Time.deltaTime=dt;UpdateDroppedItems();}
    public void ClearDrops(){ResetDroppedItems();}
}
public partial class Player
{
    public bool isLocalPlayer;
    public bool PlaceForCheck()=>RequestBlockPlacement(new Vector3Int(),BlockType.Stone,0);
    public void PlacementReplyForCheck(int request,bool accepted)=>TargetPlacementResult(null,request,accepted);
    public void DropAction(bool dropping,int id){RequestDropAction(dropping,id);}
    public void ReplayDrop(int request,bool dropping,int id,InventoryStack[] snapshot){ProcessDropAction(request,dropping,id,snapshot,false);}
    public void PendingDrop(int request){dropRequestId=request;inventory.DropRequestPending=true;}
}
public partial class PlayerInventory
{
    static void CheckDrops()
    {
        var spatialDrops=new DroppedItemStore();var nearbyDrops=new System.Collections.Generic.List<DroppedItemState>();
        var nearDrop=spatialDrops.Spawn(new InventoryStack(ItemType.Stone,1),new Vector3(-.1f,0,-.1f),Vector3.zero,0,0);
        for(int i=0;i<2000;i++)spatialDrops.Spawn(new InventoryStack(ItemType.Stone,1),new Vector3(1000+i*16,0,1000),Vector3.zero,0,0);
        spatialDrops.QueryNearby(Vector3.zero,2,nearbyDrops);
        Check(nearbyDrops.Contains(nearDrop)&&spatialDrops.LastSpatialCandidates==1,"pickup query inspects one nearby drop rather than 2001 saved drops");
        nearDrop.position=new Vector3(-17,0,-17);spatialDrops.UpdatePosition(nearDrop);spatialDrops.QueryNearby(Vector3.zero,2,nearbyDrops);
        Check(nearbyDrops.Count==0,"moving drops vacate old spatial buckets");
        var dropSnapshot=nearDrop.Copy();dropSnapshot.position=Vector3.zero;spatialDrops.Apply(dropSnapshot);spatialDrops.QueryNearby(Vector3.zero,2,nearbyDrops);
        Check(nearbyDrops.Count==1&&ReferenceEquals(nearbyDrops[0],spatialDrops.Get(nearDrop.id)),"drop snapshot replaces position and identity in the spatial index");
        spatialDrops.Remove(nearDrop.id);spatialDrops.QueryNearby(Vector3.zero,2,nearbyDrops);Check(nearbyDrops.Count==0,"drop removal clears its indexed entry");
        spatialDrops.Restore(spatialDrops.Capture());spatialDrops.QueryNearby(new Vector3(1000,0,1000),2,nearbyDrops);Check(nearbyDrops.Count==1,"drop save restoration rebuilds the index");
        spatialDrops.Restore(null);spatialDrops.QueryNearby(new Vector3(1000,0,1000),2,nearbyDrops);Check(nearbyDrops.Count==0,"drop teardown clears the index");
        Console.WriteLine("PASS: drop spatial query scaling, movement, snapshot replacement, removal and restoration.");
        var placementMenu=BuildMenu();placementMenu.AddItem(ItemType.Stone,3);
        var placer=placementMenu.GetComponent<Player>();placer.networkStarted=true;Mirror.NetworkClient.active=true;Mirror.NetworkClient.ready=true;
        Check(placer.PlaceForCheck()&&placementMenu.slots[0].count==3&&placementMenu.DropRequestPending,"placement waits for server before consuming an item");
        int placementId=Mirror.NetworkClient.placement.requestId;
        placer.PlacementReplyForCheck(placementId,false);
        Check(placementMenu.slots[0].count==3&&!placementMenu.DropRequestPending,"server rejection preserves inventory and releases placement lock");
        placer.PlaceForCheck();placementId=Mirror.NetworkClient.placement.requestId;placementMenu.selectedSlotIndex=1;
        placer.PlacementReplyForCheck(placementId,true);
        Check(placementMenu.slots[0].count==2&&placementMenu.slots[1].count==0&&!placementMenu.DropRequestPending,"acceptance consumes from the original slot despite hotbar changes");
        placer.PlacementReplyForCheck(placementId,true);
        Check(placementMenu.slots[0].count==2,"duplicate placement replies never consume twice");
        placer.CompletePlacementRequest(null,placementId,true);
        bool replayAccepted;
        Check(placer.TryReplayPlacement(placementId,out replayAccepted)&&replayAccepted,"server caches successful placement acknowledgements for replay");
        Check(placer.TryReplayPlacement(placementId-1,out replayAccepted)&&!replayAccepted,"server rejects older placement request IDs without applying them");
        Mirror.NetworkClient.ready=false;Check(!placer.PlaceForCheck()&&!placementMenu.DropRequestPending,"NotReady placements never lock or consume inventory");
        Mirror.NetworkClient.ready=true;Mirror.NetworkClient.active=false;placer.networkStarted=false;
        Console.WriteLine("PASS: placement acceptance/rejection, original-slot accounting, duplicate replies and readiness guards.");
        var loot=new GameObject("MiningLoot").AddComponent<World>();
        Check(loot.MiningItemDrop(BlockType.Stone,ItemType.DiamondPickaxe).type==ItemType.Cobblestone,"production world resolves tool harvest drops");
        Check(loot.MiningItemDrop(BlockType.DiamondOre,ItemType.GoldenPickaxe).Empty,"production world suppresses insufficient-tier ore loot");
        Check(loot.MiningItemDrop(BlockType.BirchLeaves,ItemType.Shears).type==ItemType.BirchLeaves,"production world preserves sheared leaf species");
        Check(loot.MiningItemDrop(BlockType.BirchLeaves,ItemType.Shears,238).type!=ItemType.BirchLeaves,"production world forwards tool wear into special harvest");
        var store=new DroppedItemStore();
        var item=store.Spawn(new InventoryStack(ItemType.Stone,17),new Vector3(0,0,0),new Vector3(),10,0);
        Check(item!=null&&store.Count==1&&!DroppedItemRules.CanPickup(item,Vector3.zero),"fresh drops respect pickup delay");
        for(int i=0;i<10;i++)DroppedItemRules.Tick(item,(p,v)=>new Vector3(v.x,0,v.z),.6f,Vector3.zero,false,false);
        Check(DroppedItemRules.CanPickup(item,Vector3.zero)&&!DroppedItemRules.CanPickup(item,new Vector3(5,0,0)),"pickup requires expired delay and body proximity");
        var inventory=new InventoryStack[36];for(int i=0;i<36;i++)inventory[i]=new InventoryStack(ItemType.Dirt,64);
        inventory[0]=new InventoryStack(ItemType.Stone,60);
        Check(store.Pickup(item.id,Vector3.zero,inventory)==4&&inventory[0].count==64&&store.Get(item.id).stack.count==13,"partial pickup consumes exactly the free room and leaves the rest in the world");
        Check(store.Pickup(item.id,Vector3.zero,inventory)==0&&store.Count==1,"full inventory leaves drop intact");
        inventory[1]=new InventoryStack();Check(store.Pickup(item.id,Vector3.zero,inventory)==13&&store.Count==0,"remaining stack picks up once when space opens");
        Check(store.Pickup(item.id,Vector3.zero,new InventoryStack[36])==0,"a second player cannot collect the already-removed ID");
        item=store.Spawn(new InventoryStack(ItemType.DiamondPickaxe,1,123),Vector3.zero,Vector3.zero,0,2);
        inventory=new InventoryStack[36];Check(store.Pickup(item.id,Vector3.zero,inventory)==1&&inventory[0].damage==123,"durability survives dropping and pickup");
        item=store.Spawn(new InventoryStack(ItemType.Dirt,1),new Vector3(0,4,0),Vector3.zero,0,1);
        DroppedItemRules.Tick(item,(p,v)=>v,.6f,Vector3.zero,false,false);
        Check(Math.Abs(item.position.y-3.96f)<.00001f&&Math.Abs(item.motion.y+.0392f)<.00001f,"Beta gravity and drag apply per game tick");
        var ice=item.Copy();item.motion=new Vector3(.1f,-.1f,0);ice.motion=item.motion;
        DroppedItemRules.Tick(item,(p,v)=>new Vector3(v.x,0,v.z),.6f,Vector3.zero,false,false);
        DroppedItemRules.Tick(ice,(p,v)=>new Vector3(v.x,0,v.z),.98f,Vector3.zero,false,false);
        Check(Math.Abs(item.motion.x-.0588f)<.00001f&&ice.motion.x>item.motion.x,"ground drag uses block slipperiness, including ice");
        var lava=item.Copy();Check(DroppedItemRules.Tick(lava,(p,v)=>v,.6f,Vector3.zero,true,false)&&lava.health==1,"lava damages the five-health drop");
        Check(!DroppedItemRules.Tick(lava,(p,v)=>v,.6f,Vector3.zero,true,false),"a second lava hit destroys the drop");
        item.age=5999;Check(!DroppedItemRules.Tick(item,(p,v)=>v,.6f,Vector3.zero,false,false),"despawn occurs at exactly 6000 loaded ticks");
        Check(DroppedItemRules.Copies(1)==1&&DroppedItemRules.Copies(2)==2&&DroppedItemRules.Copies(6)==3&&DroppedItemRules.Copies(21)==4,"Beta stack copy thresholds");
        Check(Math.Abs(DroppedItemRules.Bob(0,0)-.1f)<.000001f&&Math.Abs(DroppedItemRules.Rotation(20,0)-57.295776f)<.00001f,"Beta hover and spin equations");
        Check(store.Spawn(new InventoryStack(ItemType.Dirt,65),Vector3.zero,Vector3.zero,0,0)==null&&store.Spawn(new InventoryStack((ItemType)99999,1),Vector3.zero,Vector3.zero,0,0)==null,"invalid or oversized entity stacks are rejected");
        store.Restore(null);item=store.Spawn(new InventoryStack(ItemType.Dirt,6),new Vector3(1,2,3),new Vector3(.1f,.2f,.3f),7,2);
        item.age=45;item.health=3;
        var saved=store.Capture();var restored=new DroppedItemStore();
        restored.Restore(JsonUtility.FromJson<DroppedItemState[]>(JsonUtility.ToJson(saved)));
        Check(restored.Count==saved.Length&&restored.Get(saved[0].id).age==saved[0].age,"world serialization preserves independent drop state");
        var invalid=saved[0].Copy();invalid.position=new Vector3(float.NaN,0,0);restored.Restore(new[]{invalid});Check(restored.Count==0,"corrupt nonfinite saves are ignored");
        var inv=BuildMenu();var world=new GameObject("DropsWorld").AddComponent<World>();World.Instance=world;
        Check(world.SpawnItemDrop(new InventoryStack(ItemType.Dirt,130),new Vector3(0,2,0))&&world.droppedItems.Items.Sum(d=>d.stack.count)==130&&world.droppedItems.Count==3,"spawning splits oversized quantities into legal independent stacks");
        var source=world.CaptureDroppedItems();world.RestoreDroppedItems(source);world.ClearDrops();world.StepDrops(.05f);
        Check(world.droppedItems.Count==3,"deferred saved drops survive world startup clearing");
        var before=source[0].position.y;Check(source[0].position.y==before&&world.droppedItems.Get(source[0].id).position.y!=before,"save snapshots do not alias simulation motion");
        world.ClearDrops();
        var cactusCell=new Vector3Int(0,1,0);world.blocks[cactusCell]=new Block(BlockType.Cactus,cactusCell,null);
        world.SpawnItemDrop(new InventoryStack(ItemType.Stone,1),(Vector3)cactusCell+new Vector3(.5f,.1f,.5f),Vector3.zero,0);world.StepDrops(.05f);
        Check(world.droppedItems.Count==0,"cactus contact destroys a thrown item immediately");world.blocks.Remove(cactusCell);
        var player=inv.GetComponent<Player>();player.transform.position=Vector3.zero;
        player.networkStarted=true;Mirror.NetworkClient.active=true;Mirror.NetworkClient.ready=false;
        player.DropAction(true,0);
        Check(!inv.DropRequestPending&&world.droppedItems.Count==0,"NotReady drop requests do not send commands or lock inventory");
        inv.SetCursor(new InventoryStack(ItemType.Stone,2));player.RequestInventoryCursorDrop(false);
        Check(!inv.DropRequestPending&&inv.pickedUpSlot.count==2,"NotReady cursor drops preserve the held stack without locking inventory");
        inv.SetCursor(new InventoryStack());player.networkStarted=false;Mirror.NetworkClient.active=false;Mirror.NetworkClient.ready=true;
        WorldLoadingScreen.IsLoading=true;player.DropAction(true,0);
        Check(!inv.DropRequestPending,"loading screen blocks transfers even before network startup");WorldLoadingScreen.IsLoading=false;
        inv.AddItem(ItemType.DiamondPickaxe,1,damage:67);player.DropAction(true,0);
        Check(inv.slots[0].count==0&&!inv.DropRequestPending&&world.droppedItems.Count==1,"Q atomically moves one selected item into the world");
        item=world.droppedItems.Items.Single();Check(item.pickupDelay==40&&item.stack.damage==67,"thrown items preserve durability and forty-tick delay");
        player.ReplayDrop(1,true,0,new[]{new InventoryStack(ItemType.DiamondPickaxe,1)});
        Check(world.droppedItems.Count==1,"replaying an already-processed drop request never spawns twice");
        item.pickupDelay=0;item.position=Vector3.zero;
        player.DropAction(false,item.id);Check(inv.slots[0].damage==67&&world.droppedItems.Count==0,"world pickup atomically restores item durability");
        player.DropAction(false,item.id);Check(inv.slots.Count(s=>s.type==ItemType.DiamondPickaxe)==1,"duplicate pickup requests cannot create a second tool");
        world.ClearDrops();inv.SetCursor(new InventoryStack(ItemType.Stone,4));player.RequestInventoryCursorDrop(true);
        Check(inv.pickedUpSlot.count==3&&world.droppedItems.Items.Single().stack.count==1,"outside right click atomically drops one carried item");
        player.RequestInventoryCursorDrop(false);
        Check(inv.pickedUpSlot==null&&world.droppedItems.Items.Sum(x=>x.stack.count)==4,"outside left click atomically drops the remaining carried stack");
        world.ClearDrops();
        world.isSaving=true;player.DropAction(true,0);Check(inv.slots[0].count==1&&world.droppedItems.Count==0,"save boundary refuses transfers on both sides");world.isSaving=false;
        inv.DropRequestPending=true;inv.OpenInventory();Check(!inv.isOpen,"inventory cannot open over an in-flight transfer snapshot");
        inv.ClickInventorySlot(0,false,false);Check(inv.slots[0].count==1&&inv.pickedUpSlot==null,"in-flight transfers block conflicting slot edits");inv.DropRequestPending=false;
        world.SpawnItemDrop(new InventoryStack(ItemType.Stone,1),new Vector3(10,1,0));item=world.droppedItems.Items.Single();item.pickupDelay=0;
        player.DropAction(false,item.id);Check(world.droppedItems.Count==1,"server rejects distant pickup requests");
        world.StepDrops(.05f);Check(world.DropVisualCount==1,"world drop builds a cached inventory-mesh visual");
        int ageBefore=world.CaptureDroppedItems()[0].age;
        world.GenerationBusy=true;world.StepDrops(.05f);
        Check(world.CaptureDroppedItems()[0].age>ageBefore,"resident drops continue during terrain/mesh generation");
        ageBefore=world.CaptureDroppedItems()[0].age;world.betaPopulationJobs=1;world.StepDrops(.05f);
        Check(world.CaptureDroppedItems()[0].age==ageBefore,"population writes retain their safety pause");
        world.betaPopulationJobs=0;world.GenerationBusy=false;
        var mesh=world.FirstDropMesh;Check(mesh.vertices.Count==24,"block drops use the shared inventory mesh geometry");
        world.ClearDrops();Check(mesh.destroyed&&world.DropVisualCount==0,"world reset disposes owned meshes and visuals");
        foreach(float dt in new[]{.05f,.02f})
        {
            world.SpawnItemDrop(new InventoryStack(ItemType.Stone,1),new Vector3(0,2,0));
            for(int i=0;i<(int)Math.Round(5/dt);i++)world.StepDrops(dt);
            Check(world.droppedItems.Items.Single().age==100,"five seconds advances exactly 100 ticks at 20 Hz and 50 Hz");world.ClearDrops();
        }
        world.SpawnItemDrop(new InventoryStack(ItemType.Stone,1),new Vector3(10,2,0));
        World.Instance.blocks[new Vector3Int(10,2,0)]=new Block(BlockType.Nothing,new Vector3Int(10,2,0),null);
        for(int i=0;i<10;i++)world.StepDrops(.05f);
        Check(world.droppedItems.Items.Single().age==0,"unloaded drop chunks pause age and movement");world.blocks.Clear();world.ClearDrops();
        var icon=new Texture2D();var data=new ItemDataSO();data.items.Add(new ItemDefinition{id=ItemType.Stick,icon=icon,maxStack=64});ItemCatalog.Reload(data);
        world.SpawnItemDrop(new InventoryStack(ItemType.Stick,22),new Vector3(0,1,0));world.StepDrops(.05f);
        Check(world.DropVisualCount==1&&ReferenceEquals(world.FirstDropMaterial.mainTexture,icon),"flat drops reuse the inventory catalog's exact image reference");
        var flat=world.droppedItems.Items.Single();flat.pickupDelay=0;
        world.PickupItemDrop(flat.id,Vector3.zero,new InventoryStack[36]);world.StepDrops(.05f);
        Check(world.droppedItems.Count==0&&world.DropVisualCount==1,"collected items retain a brief visual flight after authoritative removal");
        for(int i=0;i<4;i++)world.StepDrops(.05f);
        Check(world.DropVisualCount==0,"collection flight cleans itself up after three game ticks");
        world.ClearDrops();Check(!icon.destroyed,"drop teardown preserves shared inventory source images");ItemCatalog.Reload(null);
        Mirror.NetworkServer.active=true;Mirror.NetworkClient.active=true;Mirror.NetworkClient.ready=false;
        player.networkStarted=true;player.isLocalPlayer=true;
        inv.SetCursor(new InventoryStack(ItemType.Stone,2));player.RequestInventoryCursorDrop(false);
        Check(inv.CursorSnapshot().Empty&&!inv.DropRequestPending,"host-local cursor drop completes synchronously while NotReady");
        var localDrop=world.droppedItems.Items.OrderBy(d=>d.id).Last();
        localDrop.pickupDelay=0;localDrop.position=player.transform.position;
        player.DropAction(false,localDrop.id);
        Check(world.droppedItems.Get(localDrop.id)==null&&!inv.DropRequestPending,"host-local pickup completes without a Command or TargetRpc while NotReady");
        Mirror.NetworkServer.active=false;Mirror.NetworkClient.active=false;Mirror.NetworkClient.ready=true;
        player.networkStarted=false;player.isLocalPlayer=false;
        inv.OnDestroy();World.Instance=new World();
    }
}
