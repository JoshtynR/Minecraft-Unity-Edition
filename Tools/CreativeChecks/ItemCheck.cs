using System;
using System.Linq;
using UnityEngine;

public partial class PlayerInventory
{
 static void CheckItemUI(PlayerInventory inv)
 {
  inv.ToggleTooManyItems();
  int index=inv.creativeBlocks.IndexOf(ItemType.Stick);
  Check(index>=0&&inv.creativeCells[index].Find("ItemIcon")!=null,"standalone items have readable placeholders in the catalogue");
  var data=new ItemDataSO();var texture=new Texture2D();
  data.items.Add(new ItemDefinition{id=ItemType.Stick,displayName="Custom Stick",icon=texture,maxStack=8});
  ItemCatalog.Reload(data);inv.RefreshCreativePage();
  var icon=inv.creativeCells[index].Find("ItemIcon");
  Check(icon.GetComponent<UnityEngine.UI.RawImage>().texture==texture&&icon.gameObject.layer==5,"assigned standalone image renders on UI layer");
  inv.GiveCatalogItem(index,false);
  Check(inv.slots[0].type==ItemType.Stick&&inv.slots[0].count==8,"catalogue uses the item's configured stack limit");
  inv.HoverSlot(inv.creativeCells[index],index,true);
  Check(inv.creativeNameText.text=="Custom Stick","tooltip uses editable item display name");
  inv.SetSlotItemType(0,ItemType.Stone);
  Check(inv.inventorySlotsRect.GetChild(0).Find("ItemIcon")==null&&inv.inventorySlotsRect.GetChild(0).Find("InventoryIcon")!=null,"replacing a sprite with a block cleans up old icon");
  Check(!texture.destroyed,"shared source texture is not destroyed with inventory icons");
  ItemCatalog.Reload(null);inv.LoadInventory(null);inv.RefreshCreativePage();
  inv.ToggleTooManyItems();
 }
 static void CheckItems()
 {
  foreach(BlockType block in Enum.GetValues(typeof(BlockType)))
  {
   if(block==BlockType.Nothing||block==BlockType.Air)continue;
   var item=ItemCatalog.FromBlock(block);BlockType placed;
   Check((int)item==(int)block&&ItemCatalog.TryPlacedBlock(item,out placed)&&placed==block,"legacy block ID roundtrips through explicit item placement mapping");
  }
  BlockType unused;
  foreach(var item in new[]{ItemType.Stick,ItemType.Coal,ItemType.Charcoal,ItemType.IronIngot,ItemType.GoldIngot,ItemType.Diamond,ItemType.RedstoneDust,ItemType.Flint})
   Check(ItemCatalog.Valid(item)&&!ItemCatalog.TryPlacedBlock(item,out unused),"standalone items are registered and cannot be placed as blocks");
  Check(!ItemCatalog.Valid((ItemType)99999)&&!ItemCatalog.TryPlacedBlock((ItemType)99999,out unused),"unknown items cannot place blocks");
  var input=new InventoryStack[4];
  for(int col=0;col<2;col++)
  {
   input=new InventoryStack[4];input[col]=input[col+2]=new InventoryStack(ItemType.WoodenPlanks,1);
   var result=InventoryRules.CraftResult(input);Check(result.type==ItemType.Stick&&result.count==4,"vertical planks craft four sticks in either column");
   foreach(var fuel in new[]{ItemType.Coal,ItemType.Charcoal})
   {
    input[col]=new InventoryStack(fuel,1);input[col+2]=new InventoryStack(ItemType.Stick,1);
    result=InventoryRules.CraftResult(input);Check(result.type==ItemType.Torch&&result.count==4,"coal or charcoal above a stick crafts four torches");
   }
  }
  var data=new ItemDataSO();
  data.items.Add(new ItemDefinition{id=ItemType.Stick,maxStack=64,maxDamage=100,placedBlock=BlockType.Nothing});
  data.items.Add(new ItemDefinition{id=ItemType.Coal,maxStack=8});
  data.items.Add(new ItemDefinition{id=ItemType.Coal,maxStack=64});
  data.items.Add(new ItemDefinition{id=(ItemType)99999,maxStack=64});
  ItemCatalog.Reload(data);
  Check(ItemCatalog.StackLimit(ItemType.Stick)==1&&ItemCatalog.StackLimit(ItemType.Coal)==8,"durable items force limit one, duplicate definitions cannot overwrite first");
  var a=new InventoryStack(ItemType.Stick,1,7);var b=new InventoryStack(ItemType.Stick,1,9);
  InventoryRules.Click(ref a,ref b,false);Check(a.damage==9&&b.damage==7&&a.count==1&&b.count==1,"different durability stacks swap without merging");
  var cells=new InventoryStack[36];cells[0]=a;
  Check(InventoryRules.Capacity(cells,ItemType.Stick,0,2,7)==1,"capacity respects stack limit and damage");
  InventoryRules.Shift(cells,0);Check(cells[9].damage==9&&cells[9].count==1&&cells[0].Empty,"shift transfer preserves damage");
  var inv=new GameObject("ItemInventory").AddComponent<PlayerInventory>();
  Check(inv.AddItem(ItemType.Stick,2,damage:23)&&inv.slots[0].count==1&&inv.slots[1].count==1&&inv.slots[1].damage==23,"atomic add distributes unstackable items and damage");
  var saved=inv.SavedInventory();inv.LoadInventory(saved);
  Check(inv.slots[0].damage==23&&inv.slots[1].damage==23,"save snapshot and load retain item damage");
  inv.ClickInventorySlot(0,false,false);inv.ClickInventorySlot(36,false,false);
  inv.ReturnTransientStacks();Check(inv.slots.Any(x=>x.type==ItemType.Stick&&x.damage==23)&&inv.craftingSlots[0].count==0,"closing crafting returns items with damage intact");
  inv.LoadInventory(new[]{new Slot{type=ItemType.Stick,count=3,damage=41}});
  Check(inv.slots[0].count==1&&inv.slots[0].damage==41&&inv.slots[1].count==0,"oversized saved stacks clamp without injecting recovery items");
  inv.LoadInventory(null);
  Check(inv.AddItem(ItemType.Coal,17)&&inv.slots[0].count==8&&inv.slots[1].count==8&&inv.slots[2].count==1,"editable stack limits govern insertion");
  Check(!inv.AddItem((ItemType)99999)&&!inv.AddItem(ItemType.Coal,1,damage:-1),"invalid grants are rejected");
  inv.LoadInventory(null);
  Check(inv.AddItem(BlockType.Dirt)&&inv.slots[0].type==ItemType.Dirt,"legacy block-drop API converts to an item counterpart");
  data.items.Add(new ItemDefinition{id=ItemType.Torch,maxStack=1});ItemCatalog.Reload(data);
  inv.LoadInventory(null,new[]{new Slot{type=ItemType.Coal,count=1},new Slot(),new Slot{type=ItemType.Stick,count=1}});
  inv.TakeCraftResult(false);
  Check(inv.pickedUpSlot==null&&inv.craftingSlots[0].count==1,"craft output larger than configured cursor limit consumes nothing");
  inv.TakeCraftResult(true);
  Check(inv.slots.Take(4).All(x=>x.type==ItemType.Torch&&x.count==1)&&inv.craftingSlots[0].count==0,"shift crafting splits output across item-limited slots");
  ItemCatalog.Reload(null);
 }
}
