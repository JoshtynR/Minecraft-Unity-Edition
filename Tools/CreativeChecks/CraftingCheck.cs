using System;
using System.Linq;
using UnityEngine;
public partial class PlayerInventory
{
    static string RecipeKey(string kind,int width,int height,ItemType output,int count,ItemType[] ingredients)
    {
        var names=ingredients.Select(i=>i.ToString());
        if(kind=="shapeless")names=names.OrderBy(i=>i);
        return kind+":"+width+":"+height+":"+output+":"+count+":"+string.Join(",",names);
    }
    static void CheckCraftingReference(string path)
    {
        var remaining=BetaCraftingRecipes.All.Select(r=>RecipeKey(r.Shapeless?"shapeless":"shaped",r.width,r.height,r.output.type,r.output.count,r.ingredients)).ToList();
        foreach(string line in System.IO.File.ReadAllLines(path).Skip(1))
        {
            var fields=line.Split('\t');bool shapeless=fields[0]=="shapeless";
            int width=int.Parse(fields[1]),height=int.Parse(fields[2]),count=int.Parse(fields[4]);
            var output=(ItemType)Enum.Parse(typeof(ItemType),fields[3]);
            var items=fields[5].Split(',').Select(i=>(ItemType)Enum.Parse(typeof(ItemType),i)).ToArray();
            Check(remaining.Remove(RecipeKey(fields[0],width,height,output,count,items)),"recipe definition exactly matches independent Beta source fixture");
            foreach(int size in new[]{2,3})
            {
                if(shapeless&&items.Length>size*size||!shapeless&&(width>size||height>size))continue;
                for(int mirror=0;mirror<2;mirror++)for(int oy=0;oy<=(shapeless?0:size-height);oy++)for(int ox=0;ox<=(shapeless?0:size-width);ox++)
                {
                    var input=new InventoryStack[size*size];
                    if(shapeless)
                        for(int i=0;i<items.Length;i++)input[mirror==0?i:input.Length-1-i]=new InventoryStack(items[i],2);
                    else for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                        input[ox+x+(oy+y)*size]=new InventoryStack(items[(mirror==1?width-1-x:x)+y*width],2);
                    var result=InventoryRules.CraftResult(input);
                    Check(result.type==output&&result.count==count,"source recipe matches at every offset and horizontal mirror in each eligible grid");
                    var before=(InventoryStack[])input.Clone();InventoryRules.ConsumeRecipe(input);
                    for(int i=0;i<input.Length;i++)Check(before[i].Empty?input[i].Empty:
                        before[i].type==ItemType.MilkBucket?input[i].type==ItemType.Bucket&&input[i].count==1:
                        input[i].type==before[i].type&&input[i].count==1,"consumption preserves empty cells and returns containers");
                    int empty=Array.FindIndex(before,i=>i.Empty);
                    if(empty>=0)
                    {
                        before[empty]=new InventoryStack(ItemType.Bedrock,1);
                        Check(InventoryRules.CraftResult(before).Empty,"unrelated extra ingredients invalidate a recipe");
                    }
                }
            }
        }
        Check(remaining.Count==0,"all 151 recipes are covered by the source fixture with no extras");
        Check(InventoryRules.CraftResult(null).Empty&&InventoryRules.CraftResult(new InventoryStack[5]).Empty,"invalid grid sizes have no recipe");
        var invalid=new[]{new InventoryStack(ItemType.Diamond,1),new InventoryStack(),new InventoryStack(),new InventoryStack()};
        InventoryRules.ConsumeRecipe(invalid);Check(invalid[0].count==1,"invalid recipes never consume inputs");
        var dye=new[]{new InventoryStack(ItemType.BoneMeal,1),new InventoryStack(ItemType.RedWool,1),new InventoryStack(),new InventoryStack()};
        Check(InventoryRules.CraftResult(dye).Empty,"Beta dye recipes require white wool rather than recoloring colored wool");
        var stew=new[]{new InventoryStack(ItemType.RedMushroom,1),new InventoryStack(ItemType.BrownMushroom,1),new InventoryStack(ItemType.Bowl,1),new InventoryStack()};
        Check(InventoryRules.CraftResult(stew).Empty,"Beta stew uses its vertical 3x3-grid recipe rather than a modern shapeless recipe");
    }
    static void FillTable(PlayerInventory inv,ItemType type,int count)
    {for(int i=0;i<9;i++)inv.WriteStack(200+i,new InventoryStack(type,count));inv.RefreshCraftingOutput();}
    static void CheckCrafting()
    {
        foreach(var recipe in BetaCraftingRecipes.All)
        {
            var input=new InventoryStack[9];
            if(recipe.Shapeless)
                for(int i=0;i<recipe.ingredients.Length;i++)input[i]=new InventoryStack(recipe.ingredients[i],1);
            else for(int y=0;y<recipe.height;y++)for(int x=0;x<recipe.width;x++)
                input[x+y*3]=new InventoryStack(recipe.ingredients[x+y*recipe.width],1);
            var result=InventoryRules.CraftResult(input);
            Check(result.type==recipe.output.type&&result.count==recipe.output.count,"registered recipe matches its inputs");
        }
        var tablePosition=new Vector3Int(2,0,0);
        foreach(bool textured in new[]{false,true})
        {
            Resources.hud=new PlayerHudAssets{craftingTable=textured?new Texture2D{width=512,height=512}:null};
            var inv=BuildMenu();World.Instance.blocks[tablePosition]=new Block(BlockType.CraftingTable,tablePosition,null);
            inv.OpenCraftingTable(tablePosition);
            Check(inv.tableOpen&&inv.isOpen&&!inv.inventoryRect.gameObject.activeSelf,"right-click table opens a separate 3x3 menu");
            Check(inv.inventorySlotsRect.parent==inv.tablePanel&&inv.craftResultRect.parent==inv.tablePanel,"table reuses the player's inventory and output");
            Check(inv.tablePanel.anchoredPosition.x==0&&!inv.creativePanel.gameObject.activeSelf,"crafting table opens centered by default");
            CheckContainerLabel(inv,inv.tablePanel,"CraftingTitle",28,6);
            CheckContainerLabel(inv,inv.tablePanel,"InventoryTitle",8,72);
            inv.ToggleTooManyItems();Check(inv.creativePanel.gameObject.activeSelf&&inv.tablePanel.anchoredPosition.x==inv.inventoryRect.anchoredPosition.x,"F1 reflows open crafting table with catalogue");
            inv.ToggleTooManyItems();Check(inv.tablePanel.anchoredPosition.x==0,"hiding catalogue recenters open crafting table");
            if(textured)
            {
                var bg=inv.tablePanel.GetComponent<UnityEngine.UI.RawImage>();
                Check(bg.texture==Resources.hud.craftingTable&&bg.uvRect.width==176/256f&&bg.uvRect.height==166/256f&&bg.uvRect.y==90/256f,"table crops the correct panel from HD artwork");
            }
            for(int i=0;i<9;i++)Check(inv.tableRects[i].GetComponent<InventorySlot>().slotIndex==200+i,"all nine cells have input handlers");
            inv.SetCursor(new InventoryStack(ItemType.Cobblestone,8));
            for(int i=0;i<9;i++)if(i!=4)inv.ClickInventorySlot(200+i,true,false);
            Check(inv.craftedOutput.type==ItemType.Furnace&&inv.craftedOutput.count==1,"placing furnace shape updates live output");
            inv.ClickInventorySlot(40,false,false);
            Check(inv.pickedUpSlot.type==ItemType.Furnace&&inv.tableSlots.All(s=>s.count==0),"taking furnace consumes only ingredients");
            inv.CloseInventory();Check(!inv.tableOpen&&!inv.isOpen&&inv.slots[0].type==ItemType.Furnace,"closing returns cursor and restores survival inventory");
            Check(inv.craftResultRect.parent==inv.inventoryRect&&inv.craftResultRect.anchoredPosition.x==74,"closing restores inventory output control");
            inv.LoadInventory(null);
            inv.OpenCraftingTable(tablePosition);
            FillTable(inv,ItemType.IronIngot,2);
            Check(inv.craftedOutput.type==ItemType.IronBlock,"nine ingots preview a compressed block");
            inv.SetCursor(new InventoryStack(ItemType.IronBlock,63));inv.ClickInventorySlot(40,false,false);
            Check(inv.pickedUpSlot.count==64&&inv.tableSlots.All(s=>s.count==1),"cursor crafting fills only available room");
            inv.ClickInventorySlot(40,false,false);
            Check(inv.tableSlots.All(s=>s.count==1),"full cursor cannot consume more inputs");
            inv.SetCursor(new InventoryStack());
            for(int i=0;i<36;i++)inv.WriteStack(i,new InventoryStack(ItemType.Dirt,64));
            inv.ClickInventorySlot(40,false,true);
            Check(inv.tableSlots.All(s=>s.count==1),"full storage stops shift crafting without consuming anything");
            var saved=inv.SavedRecovery();Check(saved.Length==9&&saved.All(s=>s.type==ItemType.IronIngot&&s.count==1),"saving an open table snapshots each input exactly once");
            inv.tableSlots[0].count=2;Check(saved[0].count==1,"table save snapshots do not alias live inputs");inv.tableSlots[0].count=1;
            inv.CloseInventory();Check(inv.recoveryItems.Count==9&&inv.tableSlots.All(s=>s.count==0),"closing with full storage preserves ingredients in recoverable saves");
            var stored=inv.SavedInventory();inv.LoadInventory(stored,recovery:saved);
            inv.OpenCraftingTable(tablePosition);Check(inv.tableSlots.All(s=>s.count==0)&&inv.recoveryItems.Count==9,"restoring a table snapshot never also populates a duplicate table grid");
            inv.RemoveItem(0);inv.RecoverSavedItems();Check(inv.slots[0].type==ItemType.IronIngot&&inv.slots[0].count==9,"saved table inputs recover into free storage");
            inv.CloseInventory();inv.LoadInventory(null);inv.OpenCraftingTable(tablePosition);
            FillTable(inv,ItemType.GoldIngot,4);inv.ClickInventorySlot(40,false,true);
            Check(inv.slots.Sum(s=>s.type==ItemType.GoldBlock?s.count:0)==4&&inv.tableSlots.All(s=>s.count==0),"shift output repeats only while ingredients and capacity remain");
            inv.SetCursor(new InventoryStack(ItemType.DiamondPickaxe,1,17));inv.ClickInventorySlot(200,false,false);inv.ClickInventorySlot(200,false,true);
            Check(inv.slots.Any(s=>s.type==ItemType.DiamondPickaxe&&s.damage==17),"shift-removing a table input preserves item durability");
            inv.SetCursor(new InventoryStack(ItemType.Diamond,5));inv.ClickInventorySlot(200,false,false);
            inv.transform.position=new Vector3(100,0,0);inv.UpdateCraftingTable();
            Check(!inv.isOpen&&!inv.tableOpen&&inv.slots.Any(s=>s.type==ItemType.Diamond&&s.count==5),"leaving table range closes safely and returns its contents");
            inv.transform.position=Vector3.zero;inv.OpenCraftingTable(tablePosition);
            World.Instance.blocks[tablePosition]=new Block(BlockType.Air,tablePosition,null);inv.ClickInventorySlot(40,false,true);
            Check(!inv.isOpen&&!inv.tableOpen,"destroyed table cannot be used through stale output clicks");
            World.Instance.blocks[tablePosition]=new Block(BlockType.CraftingTable,tablePosition,null);
            Mirror.NetworkClient.active=true;inv.OpenCraftingTable(tablePosition);
            Mirror.NetworkClient.active=false;inv.UpdateCraftingTable();
            Check(!inv.tableOpen&&!inv.isOpen,"disconnect closes a network crafting session");
            var sheet=Resources.hud.craftingTable;
            inv.OnDestroy();Check(sheet==null||!sheet.destroyed,"table cleanup keeps shared artwork alive");
        }
        Resources.hud=null;
    }
}
