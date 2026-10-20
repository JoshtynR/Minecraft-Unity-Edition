using System;
using System.Linq;
using UnityEngine;

// Fluid/collision dependencies have their own production parity suites. Here the
// ground probe supplies collision results to exercise the real drop update loop.
public static class BlockCollision
{
    public static Vector3 Resolve(World world,Vector3 feet,float radius,float height,Vector3 move,bool step)
    {if(feet.y+move.y<0)move.y=-feet.y;return move;}
}
public static class FluidWorldQueries
{
    public static int Sample(World world,WaterPosition p,BlockType type)=>-1;
    public static bool SolidFace(World world,WaterPosition p)=>false;
}

public partial class World
{
    public int DropVisualCount=>dropVisuals.Count;public Mesh FirstDropMesh=>dropMeshes.Values.First();public Material FirstDropMaterial=>dropMaterials.Values.First();
    public void StepDrops(float dt){Time.deltaTime=dt;UpdateDroppedItems();}
    public void ClearDrops(){ResetDroppedItems();}
}
public partial class Player
{
    public void DropAction(bool dropping,int id){RequestDropAction(dropping,id);}
    public void ReplayDrop(int request,bool dropping,int id,InventoryStack[] snapshot){ProcessDropAction(request,dropping,id,snapshot,false);}
    public void PendingDrop(int request){dropRequestId=request;inventory.DropRequestPending=true;}
}
public partial class PlayerInventory
{
    static void CheckDrops()
    {
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
        var player=inv.GetComponent<Player>();player.transform.position=Vector3.zero;
        inv.AddItem(ItemType.DiamondPickaxe,1,damage:67);player.DropAction(true,0);
        Check(inv.slots[0].count==0&&!inv.DropRequestPending&&world.droppedItems.Count==1,"Q atomically moves one selected item into the world");
        item=world.droppedItems.Items.Single();Check(item.pickupDelay==40&&item.stack.damage==67,"thrown items preserve durability and forty-tick delay");
        player.ReplayDrop(1,true,0,new[]{new InventoryStack(ItemType.DiamondPickaxe,1)});
        Check(world.droppedItems.Count==1,"replaying an already-processed drop request never spawns twice");
        item.pickupDelay=0;item.position=Vector3.zero;
        player.DropAction(false,item.id);Check(inv.slots[0].damage==67&&world.droppedItems.Count==0,"world pickup atomically restores item durability");
        player.DropAction(false,item.id);Check(inv.slots.Count(s=>s.type==ItemType.DiamondPickaxe)==1,"duplicate pickup requests cannot create a second tool");
        world.isSaving=true;player.DropAction(true,0);Check(inv.slots[0].count==1&&world.droppedItems.Count==0,"save boundary refuses transfers on both sides");world.isSaving=false;
        inv.DropRequestPending=true;inv.OpenInventory();Check(!inv.isOpen,"inventory cannot open over an in-flight transfer snapshot");
        inv.ClickInventorySlot(0,false,false);Check(inv.slots[0].count==1&&inv.pickedUpSlot==null,"in-flight transfers block conflicting slot edits");inv.DropRequestPending=false;
        world.SpawnItemDrop(new InventoryStack(ItemType.Stone,1),new Vector3(10,1,0));item=world.droppedItems.Items.Single();item.pickupDelay=0;
        player.DropAction(false,item.id);Check(world.droppedItems.Count==1,"server rejects distant pickup requests");
        world.StepDrops(.05f);Check(world.DropVisualCount==1,"world drop builds a cached inventory-mesh visual");
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
        inv.OnDestroy();World.Instance=new World();
    }
}
