using System;
using System.Linq;
using UnityEngine;

namespace UnityEngine
{
 public struct Bounds
 {
  public Vector3 center,size;
  public Bounds(Vector3 center,Vector3 size){this.center=center;this.size=size;}
 }
}
public partial class PlayerInventory
{
 static void CheckCows()
 {
  var store=new CowStore();var cow=store.Spawn(new Vector3(2,64,-3),90);
  Check(CowRules.DyeColor(ItemType.InkSac)==15&&CowRules.DyeColor(ItemType.BoneMeal)==0&&CowRules.DyeColor(ItemType.Wheat)==-1,"dye metadata maps to wool color without treating food as dye");
  Check(CowRules.Wool(new CowState{fleeceColor=15})==ItemType.BlackWool&&CowRules.Wool(new CowState())==ItemType.WhiteWool,"colored sheep drop matching wool");
  foreach(PassiveAnimal species in Enum.GetValues(typeof(PassiveAnimal)))
  {
   var herd=new CowStore();var a=herd.Spawn(Vector3.zero,0,species);var b=herd.Spawn(Vector3.zero,0,species);
   Check(a.species==species&&CowRules.Feed(a)&&CowRules.Feed(b)&&CowRules.CanBreed(a,b),"all passive species accept feeding and same-species breeding");
   Check(!CowRules.Feed(a),"already loving animals cannot consume another breeding item");
   b.ageTicks=24000;Check(!CowRules.CanBreed(a,b)&&!CowRules.Feed(b),"babies cannot breed");
   b.ageTicks=0;b.breedCooldown=6000;Check(!CowRules.CanBreed(a,b),"breeding cooldown prevents immediate repeat births");
   Check(CowRules.BreedingFood(a)==(species==PassiveAnimal.Chicken?ItemType.Seeds:ItemType.Wheat),"chickens use seeds; other farm animals use wheat");
   var data=new CowSaveData{animals=herd.Capture()};var loaded=new CowStore();loaded.Restore(JsonUtility.FromJson<CowSaveData>(JsonUtility.ToJson(data)));
   Check(loaded.Get(b.id).species==species&&loaded.Get(b.id).breedCooldown==6000,"species and breeding state survive saves");
   for(int part=0;part<CowModelGeometry.PartCount;part++)
   {
    var model=CowModelGeometry.Build(part,32,species);
    foreach(var uv in model.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"passive animal UVs stay within the skin");
   }
  }
  Check(cow.id==1&&cow.health==10&&store.Count==1,"cow spawns with ten health and a persistent identity");
  Check(CowRules.Bounds(cow).size.x==.9f&&CowRules.Bounds(cow).size.y==1.3f,"classic cow collision size");
  var capture=store.Capture();capture[0].position.x=900;
  Check(cow.position.x==2,"cow save snapshot does not alias live position");
  var save=new CowSaveData{animals=store.Capture(),nextId=store.NextId,populatedChunks=new[]{new Vector3Int(-16,0,16)},randomState=12345,hasRandomState=true};
  var restored=new CowStore();restored.Restore(JsonUtility.FromJson<CowSaveData>(JsonUtility.ToJson(save)));
  Check(restored.Get(1).position.z==-3&&restored.Get(1).yaw==90,"cow identity and transform survive serialization");
  Check(restored.Remove(1)&&!restored.Remove(1)&&restored.Spawn(Vector3.zero,0).id==2,"removal happens once and saved identities are never reused");
  Check(!CowRules.Valid(new CowState{id=8,position=new Vector3(float.NaN,0,0)}),"invalid cow positions are rejected");
  restored.Restore(new CowSaveData{animals=new[]{new CowState{id=3,health=0},new CowState{id=4},new CowState{id=4}}});
  Check(restored.Count==1&&restored.NextId==5,"restoring rejects dead and duplicate cows");
  var bounded=new CowStore();
  for(int i=0;i<CowStore.Capacity;i++)Check(bounded.Spawn(Vector3.zero,0)!=null,"bounded cow store accepts valid capacity");
  Check(bounded.Spawn(Vector3.zero,0)==null&&bounded.Count==CowStore.Capacity,"cow capacity prevents unbounded population");
  cow=new CowState{id=1};
  Check(CowRules.Hurt(cow,1,new Vector3(1,0,0))&&cow.health==9&&cow.fleeTicks==100&&cow.motion.y>0,"punch damages cow and starts flee/knockback");
  Check(!CowRules.Hurt(cow,10,Vector3.zero)&&cow.health==9,"hurt cooldown prevents duplicate damage");
  cow.hurtCooldown=0;Check(CowRules.Hurt(cow,10,Vector3.zero)&&cow.health==0&&!CowRules.Hurt(cow,1,Vector3.zero),"dead cows cannot take damage again");
  Check(CowRules.AttackDamage(ItemType.Nothing)==1&&CowRules.AttackDamage(ItemType.WoodenSword)==4&&CowRules.AttackDamage(ItemType.StoneSword)==6&&CowRules.AttackDamage(ItemType.IronSword)==8&&CowRules.AttackDamage(ItemType.DiamondSword)==10,"Beta sword and hand damage");
  Check(CowRules.AttackDamage(ItemType.DiamondAxe)==6&&CowRules.AttackDamage(ItemType.DiamondPickaxe)==5&&CowRules.AttackDamage(ItemType.DiamondShovel)==4,"Beta tool attack damage");
  for(int roll=0;roll<3;roll++){int r=roll;Check(CowRules.LeatherCount(bound=>{Check(bound==3,"leather RNG bound");return r;})==roll,"Beta leather range includes zero");}
  var bag=new InventoryStack[36];bag[0]=new InventoryStack(ItemType.Bucket,1);
  Check(CowRules.Milk(bag,0)&&bag[0].type==ItemType.MilkBucket&&bag[0].count==1,"empty bucket becomes milk bucket in the selected slot");
  Check(!CowRules.Milk(bag,0)&&!CowRules.Milk(bag,-1),"milk cannot overwrite an unrelated item");
  bag=new InventoryStack[36];bag[0]=new InventoryStack(ItemType.Bucket,2);
  Check(CowRules.Milk(bag,0)&&bag[0].count==1&&bag.Count(s=>s.type==ItemType.MilkBucket)==1,"custom stacked buckets preserve remaining buckets");
  for(int i=0;i<36;i++)bag[i]=new InventoryStack(ItemType.Stone,64);
  bag[0]=new InventoryStack(ItemType.Bucket,2);
  Check(!CowRules.Milk(bag,0)&&bag[0].count==2,"full inventory refuses stacked milking without consuming a bucket");
  int vertices=0;
  for(int part=0;part<CowModelGeometry.PartCount;part++)
  {
   var model=CowModelGeometry.Build(part);vertices+=model.vertices.Count;
   Check(model.vertices.Count==24&&model.triangles.Count==36&&model.uv.Count==24,"each model box has six complete textured faces");
   foreach(var uv in model.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"cow UVs stay on the classic 64 by 32 skin");
   foreach(int triangle in model.triangles)Check(triangle>=0&&triangle<24,"cow triangle indices reference local box vertices");
   var center=new Vector3(model.vertices.Average(p=>p.x),model.vertices.Average(p=>p.y),model.vertices.Average(p=>p.z));
   for(int face=0;face<6;face++)
   {
    var a=model.vertices[face*4];var b=model.vertices[face*4+1];var c=model.vertices[face*4+2];
    var ab=b-a;var ac=c-a;var outward=a-center;
    var n=new Vector3(ab.y*ac.z-ab.z*ac.y,ab.z*ac.x-ab.x*ac.z,ab.x*ac.y-ab.y*ac.x);
    Check(n.x*outward.x+n.y*outward.y+n.z*outward.z>0,"cow winding faces outward for backface culling");
   }
  }
  Check(vertices==216,"cow includes body, head, two horns, udder and four legs");
  Check(CowModelGeometry.Pivot(5).x==-.25f&&CowModelGeometry.Pivot(7).z==.375f,"cow legs use classic widened and front-shifted pivots");
  var head=CowModelGeometry.Build(1);
  Check(Math.Abs(head.uv[16].x-(13.9f/64))<.000001f&&Math.Abs(head.uv[16].y-(1-6.1f/32))<.000001f,"head front samples face strip rather than back-of-head texture");
 }
}
