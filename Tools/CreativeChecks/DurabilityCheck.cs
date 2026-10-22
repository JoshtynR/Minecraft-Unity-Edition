using System;
using System.Linq;
using UnityEngine;

public partial class Player
{
    public float exhaustion;
    public void AddForce(Vector3 direction,float amount){}
    public void RpcAddForce(Mirror.NetworkConnection connection,Vector3 direction,float amount){}
    public void RestoreHitFixture(){dropRequestId=lastServerDropRequest=1;Mirror.NetworkTime.time=0;}
    public bool ServerMiningForCheck=>serverMining;
    public void WearRevisionForCheck(int server,int client){serverItemWearRevision=server;knownItemWearRevision=client;}
    public void ReplayDropWithWearRevision(int request,bool dropping,int id,InventoryStack[] snapshot,int revision){ProcessDropAction(request,dropping,id,snapshot,false,revision);}
    public void HitForDurability(Player target){WearItemOnHit(target);}
    public void ReplayHitForDurability(int request,Player target,int slot,InventoryStack[] snapshot){ProcessItemHitWear(request,target,slot,snapshot,false);}
}
namespace Mirror {public static class NetworkTime {public static double time;}}
public partial class PlayerInventory
{
    static void CheckDurability()
    {
        foreach(var definition in ItemCatalog.All)
        {
            int max=definition.maxDamage;
            var item=new InventoryStack(definition.id,1,Math.Max(0,max-1));
            if(max==0)
            {
                Check(!ItemDurability.Wear(ref item,1)&&item.damage==0,"non-durable items and Beta bows never acquire wear");continue;
            }
            Check(!ItemDurability.Wear(ref item,1)&&item.damage==max&&item.count==1,"Beta keeps a durable item at exactly its maximum damage value");
            Check(ItemDurability.BarWidth(item)==0&&ItemDurability.BarGreen(item)==0,"last remaining use has an empty red durability bar");
            Check(ItemDurability.Wear(ref item,1)&&item.Empty&&item.damage==0,"the next wear point breaks every catalog durable type and normalizes its slot");
        }
        Check(ItemDurability.Maximum(ItemType.WoodenPickaxe)==59&&ItemDurability.Maximum(ItemType.DiamondPickaxe)==1561&&ItemDurability.Maximum(ItemType.GoldenPickaxe)==32,"source tier limits remain intact");
        foreach(var type in new[]{ItemType.WoodenPickaxe,ItemType.IronAxe,ItemType.GoldenShovel})
            Check(ItemDurability.MiningWear(type,BlockType.Torch)==1&&ItemDurability.HitWear(type)==2,"Beta tools wear one for blocks including instant breaks and two for entity hits");
        Check(ItemDurability.MiningWear(ItemType.DiamondSword,BlockType.Dirt)==2&&ItemDurability.HitWear(ItemType.DiamondSword)==1,"swords have the opposite block/hit wear costs");
        foreach(var leaf in new[]{BlockType.Leaves,BlockType.SpruceLeaves,BlockType.BirchLeaves,BlockType.Cobweb})
            Check(ItemDurability.MiningWear(ItemType.Shears,leaf)==1,"shears wear on leaves and webs");
        Check(ItemDurability.MiningWear(ItemType.Shears,BlockType.WhiteWool)==0&&ItemDurability.MiningWear(ItemType.IronHoe,BlockType.Dirt)==0,"Beta shears do not wear on wool, and hoes do not wear when breaking blocks");
        Check(ItemDurability.FishingWear(false,false,false)==0&&ItemDurability.FishingWear(false,true,false)==1&&ItemDurability.FishingWear(true,false,false)==3&&ItemDurability.FishingWear(true,true,true)==2,"future fishing hook follows catch/retract source costs and stuck precedence");
        var inv=BuildMenu();var expected=new InventoryStack(ItemType.IronPickaxe,1);
        inv.WriteStack(0,expected);Check(inv.hotbarRect.GetChild(1).Find("DurabilityBar")==null,"pristine items do not draw a durability bar");
        inv.ApplyItemWear(0,expected,1);
        var bar=inv.inventorySlotsRect.GetChild(0).Find("DurabilityBar");
        Check(bar!=null&&bar.gameObject.activeSelf&&inv.hotbarRect.GetChild(1).Find("DurabilityBar").gameObject.activeSelf,"wear updates both inventory and hotbar bars");
        inv.ApplyItemWear(0,expected,1);Check(inv.slots[0].damage==1,"replayed expected-value wear cannot charge an item twice");
        inv.ClickInventorySlot(0,false,false);
        Check(inv.pickedUpSlot.damage==1&&inv.pickedUpItemRect.Find("DurabilityBar").gameObject.activeSelf,"carried items show their existing wear");
        inv.ClickInventorySlot(1,false,false);inv.ClickInventorySlot(1,false,true);
        Check(inv.slots[9].damage==1&&inv.inventorySlotsRect.GetChild(9).Find("DurabilityBar").gameObject.activeSelf,"shift transfer preserves wear and updates its destination bar");
        UnityEngine.Object.deferDestroy=true;
        inv.WriteStack(0,new InventoryStack(ItemType.IronSword,1,5));inv.WriteStack(0,new InventoryStack(ItemType.DiamondSword,1,10));
        var replacementBar=inv.inventorySlotsRect.GetChild(0).Find("DurabilityBar");
        Check(replacementBar!=null&&!replacementBar.gameObject.destroyed,"replacing a worn item creates a live bar even with Unity's deferred destruction");
        UnityEngine.Object.deferDestroy=false;
        var save=inv.SavedInventory();inv.LoadInventory(save);Check(inv.slots[9].damage==1,"wear survives saved inventory snapshots");
        inv.WriteStack(0,new InventoryStack(ItemType.WoodenPickaxe,1,59));
        inv.ApplyItemWear(0,Stack(inv.slots[0]),1);
        Check(inv.slots[0].count==0&&inv.hotbarRect.GetChild(1).Find("DurabilityBar")==null,"breaking removes the icon and durability bar together");
        var mine=inv.GetComponent<Player>();var target=new GameObject("HitTarget",typeof(Player)).GetComponent<Player>();
        target.transform.position=new Vector3(0,0,2);
        inv.WriteStack(0,new InventoryStack(ItemType.WoodenSword,1));mine.HitForDurability(target);
        Check(inv.slots[0].damage==1&&!inv.DropRequestPending,"valid existing hit interaction consumes sword durability once and releases the transaction");
        Check(target.health==16,"wood sword now damages player health instead of only pushing");
        mine.HitForDurability(target);
        Check(target.health==16&&inv.slots[0].damage==1,"hurt window rejects rapid repeat damage and wear");
        Mirror.NetworkTime.time=1;
        inv.WriteStack(0,new InventoryStack());mine.HitForDurability(target);
        Check(target.health==15&&!inv.DropRequestPending,"empty hand can attack without requiring a durable tool");
        inv.WriteStack(0,new InventoryStack(ItemType.WoodenSword,1,1));
        mine.RestoreHitFixture();
        mine.ReplayHitForDurability(1,target,0,inv.DropInventorySnapshot());Check(inv.slots[0].damage==1,"replayed hit requests cannot duplicate wear");
        target.transform.position=new Vector3(100,0,0);mine.HitForDurability(target);
        Check(inv.slots[0].damage==1,"distant hit reports do not damage the tool");
        inv.LoadInventory(null);inv.WriteStack(0,new InventoryStack(ItemType.WoodenPickaxe,1));
        var moved=Stack(inv.slots[0]);inv.ClickInventorySlot(0,false,false);inv.WriteStack(0,new InventoryStack(ItemType.IronPickaxe,1));
        inv.ApplyCompletedMiningWear(0,moved,1);
        Check(inv.slots[0].damage==0&&inv.pickedUpSlot.damage==1,"a late mining receipt follows its matching cursor tool rather than wearing a replacement");
        inv.LoadInventory(null);inv.QueueRecovery(ItemType.WoodenPickaxe,1);inv.ApplyCompletedMiningWear(0,new InventoryStack(ItemType.WoodenPickaxe,1),1);
        Check(inv.SavedRecovery().Single().damage==1,"late mining wear follows a tool preserved in menu overflow recovery");
        inv.LoadInventory(null);inv.WriteStack(0,new InventoryStack(ItemType.IronPickaxe,1,1));
        var stale=inv.DropInventorySnapshot();stale[0].damage=0;
        var world=World.Instance;world.ClearDrops();world.SpawnItemDrop(new InventoryStack(ItemType.Dirt,1),Vector3.zero,delay:0);
        int dropId=world.droppedItems.Items.Single().id;
        mine.WearRevisionForCheck(1,0);mine.PendingDrop(3);mine.ReplayDropWithWearRevision(3,false,dropId,stale,0);
        Check(inv.slots[0].damage==1&&world.droppedItems.Count==1&&!inv.DropRequestPending,"a pickup snapshot from before mining wear is rejected without reverting durability or consuming loot");
        var connection=new Mirror.NetworkConnectionToClient{identity=inv.gameObject};
        new WorldServer().ReceiveChestRequest(connection,new ChestRequest{action=0,itemWearRevision=0,inventory=stale});
        Check(!connection.last.accepted&&connection.last.error.Contains("Inventory update pending"),"stale chest-open snapshots cannot overwrite newer tool wear");
        mine.WearRevisionForCheck(1,1);mine.DropAction(false,dropId);
        Check(inv.slots[0].damage==1&&world.droppedItems.Count==0,"pickup resumes after the wear revision is acknowledged");
        inv.OnDestroy();
    }
}
