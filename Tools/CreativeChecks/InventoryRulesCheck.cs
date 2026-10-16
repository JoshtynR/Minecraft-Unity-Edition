using System;
using System.Linq;
public partial class PlayerInventory
{
 static int Amount(InventoryStack a,InventoryStack b,BlockType t)=>(!a.Empty&&a.type==t?a.count:0)+(!b.Empty&&b.type==t?b.count:0);
 static void CheckRules()
 {
  for(int same=0;same<2;same++)for(int right=0;right<2;right++)for(int a=0;a<=64;a++)for(int b=0;b<=64;b++)
  {
   var slot=new InventoryStack(BlockType.Stone,a);var cursor=new InventoryStack(same==1?BlockType.Stone:BlockType.Dirt,b);
   int stones=Amount(slot,cursor,BlockType.Stone),dirt=Amount(slot,cursor,BlockType.Dirt);
   InventoryRules.Click(ref slot,ref cursor,right==1);
   Check(Amount(slot,cursor,BlockType.Stone)==stones&&Amount(slot,cursor,BlockType.Dirt)==dirt,"click arithmetic conserves every type at all legal stack counts");
   Check(slot.count>=0&&slot.count<=64&&cursor.count>=0&&cursor.count<=64,"click arithmetic respects stack limits");
   Check((slot.count==0)==(slot.type==BlockType.Nothing)&&(cursor.count==0)==(cursor.type==BlockType.Nothing),"empty slots normalize both type and count");
  }
  var slots=new InventoryStack[36];for(int i=0;i<36;i++)slots[i]=new InventoryStack(BlockType.Dirt,64);
  slots[0]=new InventoryStack(BlockType.Stone,63);slots[9]=new InventoryStack(BlockType.Stone,62);slots[10]=new InventoryStack();
  Check(InventoryRules.Capacity(slots,BlockType.Stone,0,36)==67,"capacity sums partial stacks and empty slots");
  int left=InventoryRules.Insert(slots,BlockType.Stone,128,0,36);
  Check(left==61&&slots[0].count==64&&slots[9].count==64&&slots[10].count==64,"insertion merges partially before filling empty space without oversized stack");
  slots=new InventoryStack[36];for(int i=0;i<36;i++)slots[i]=new InventoryStack(BlockType.Dirt,64);
  slots[0]=new InventoryStack(BlockType.Stone,64);slots[9]=new InventoryStack(BlockType.Stone,63);
  InventoryRules.Shift(slots,0);Check(slots[0].count==63&&slots[9].count==64,"shift transfer leaves uninserted remainder at source");
  foreach(var log in new[]{BlockType.Log,BlockType.SpruceLog,BlockType.BirchLog})for(int cell=0;cell<4;cell++)
  {
   var input=new InventoryStack[4];input[cell]=new InventoryStack(log,1);
   var result=InventoryRules.CraftResult(input);Check(result.type==BlockType.WoodenPlanks&&result.count==4,"Beta log conversion is shapeless across all four cells");
   InventoryRules.ConsumeRecipe(input);Check(input.All(s=>s.Empty),"recipe consumption clears exhausted input");
  }
  foreach(var type in new[]{BlockType.WoodenPlanks,BlockType.Sand})
  {
   var input=Enumerable.Repeat(new InventoryStack(type,2),4).ToArray();var result=InventoryRules.CraftResult(input);
   Check(result.count==1&&result.type==(type==BlockType.Sand?BlockType.Sandstone:BlockType.CraftingTable),"supported 2x2 block recipes");
   InventoryRules.ConsumeRecipe(input);Check(input.All(s=>s.count==1),"2x2 recipe consumes one per cell");
  }
  for(int row=0;row<2;row++)foreach(var type in new[]{BlockType.WoodenPlanks,BlockType.Stone})
  {
   var input=new InventoryStack[4];input[row*2]=input[row*2+1]=new InventoryStack(type,1);
   Check(InventoryRules.CraftResult(input).type==(type==BlockType.Stone?BlockType.StonePressurePlate:BlockType.WoodenPressurePlate),"pressure plate recipe can occupy either grid row");
  }
  for(int column=0;column<2;column++)
  {
   var input=new InventoryStack[4];input[column]=input[column+2]=new InventoryStack(BlockType.Stone,1);
   Check(InventoryRules.CraftResult(input).type==BlockType.StoneButton,"Beta button requires vertical pair of stone");
   input[column]=new InventoryStack(BlockType.Pumpkin,1);input[column+2]=new InventoryStack(BlockType.Torch,1);
   Check(InventoryRules.CraftResult(input).type==BlockType.JackOLantern,"jack-o-lantern requires pumpkin above torch");
   input[column]=new InventoryStack(BlockType.Torch,1);input[column+2]=new InventoryStack(BlockType.Pumpkin,1);
   Check(InventoryRules.CraftResult(input).Empty,"reversing pumpkin and torch cannot craft");
  }
  for(int mask=0;mask<15;mask++)
  {
   var input=new InventoryStack[4];for(int i=0;i<4;i++)if((mask&(1<<i))!=0)input[i]=new InventoryStack(BlockType.Sand,1);
   Check(InventoryRules.CraftResult(input).Empty,"partial 2x2 recipe cannot generate output");
  }
  foreach(var size in new[]{new[]{640,360},new[]{427,240},new[]{320,200},new[]{256,144},new[]{1920,1080}})
  {
   var layout=TmiLayout.Calculate(size[0],size[1]);
   Check(layout.inventoryX-88>=-layout.width/2&&layout.sidebarX+layout.columns*9<=layout.width/2,"responsive layout keeps both panels within canvas");
   Check(layout.inventoryX+88+8<=layout.sidebarX-layout.columns*9,"responsive layout reserves separate sidebar");
   Check(layout.rows*18+38<=layout.height&&layout.PageSize>0,"responsive catalogue rows fit height");
  }
  Check(TmiLayout.SlotY(0)==-67&&TmiLayout.SlotY(9)==-9&&TmiLayout.SlotY(35)==-45&&TmiLayout.SlotX(35)==72,"Beta survival slot coordinates");
  foreach(BlockType type in Enum.GetValues(typeof(BlockType)))for(int armor=0;armor<4;armor++)
   Check(InventoryRules.EquipmentAccepts(armor,type)==(armor==0&&type==BlockType.Pumpkin),"equipment validity follows the available Beta block registry, including pumpkin-only exception");
 }
}
