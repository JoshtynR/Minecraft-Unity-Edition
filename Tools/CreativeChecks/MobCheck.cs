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
 static void CheckMobs()
 {
  var store=new MobStore();var mob=store.Spawn(new Vector3(2,64,-3),90);
  Check(MobRules.DyeColor(ItemType.InkSac)==15&&MobRules.DyeColor(ItemType.BoneMeal)==0&&MobRules.DyeColor(ItemType.Wheat)==-1,"dye metadata maps to wool color without treating food as dye");
  Check(MobRules.Wool(new MobState{fleeceColor=15})==ItemType.BlackWool&&MobRules.Wool(new MobState())==ItemType.WhiteWool,"colored sheep drop matching wool");
  foreach(MobType species in Enum.GetValues(typeof(MobType)))
  {
   var herd=new MobStore();var a=herd.Spawn(Vector3.zero,0,species);var b=herd.Spawn(Vector3.zero,0,species);
   if(MobRules.Hostile(species))
   {
    Check(!MobRules.Feed(a)&&!MobRules.CanBreed(a,b),"hostiles cannot consume breeding food or reproduce");
    Check(a.health==(species==MobType.Spider?16:20),"hostile species spawn with reference health");
    Check(MobRules.Valid(a)&&MobRules.BreedingFood(a)==ItemType.Nothing,"hostile snapshots validate without inheriting passive food");
    for(int part=0;part<MobModelGeometry.PartCountFor(species);part++)foreach(var uv in MobModelGeometry.Build(part,32,species).uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"hostile model UVs remain within skin");
    var restoredHostile=new MobStore();restoredHostile.Restore(new MobSaveData{animals=herd.Capture()});Check(restoredHostile.Get(a.id).species==species,"hostile identity and species survive store restore");
    continue;
   }
   Check(a.species==species&&MobRules.Feed(a)&&MobRules.Feed(b)&&MobRules.CanBreed(a,b),"all passive species accept feeding and same-species breeding");
   Check(!MobRules.Feed(a),"already loving animals cannot consume another breeding item");
   b.ageTicks=24000;Check(!MobRules.CanBreed(a,b)&&!MobRules.Feed(b),"babies cannot breed");
   b.ageTicks=0;b.breedCooldown=6000;Check(!MobRules.CanBreed(a,b),"breeding cooldown prevents immediate repeat births");
   Check(MobRules.BreedingFood(a)==(species==MobType.Chicken?ItemType.Seeds:ItemType.Wheat),"chickens use seeds; other farm animals use wheat");
   var data=new MobSaveData{animals=herd.Capture()};var loaded=new MobStore();loaded.Restore(JsonUtility.FromJson<MobSaveData>(JsonUtility.ToJson(data)));
   Check(loaded.Get(b.id).species==species&&loaded.Get(b.id).breedCooldown==6000,"species and breeding state survive saves");
   for(int part=0;part<MobModelGeometry.PartCountFor(species);part++)
   {
    var model=MobModelGeometry.Build(part,32,species);
    foreach(var uv in model.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"passive animal UVs stay within the skin");
   }
  }
  Check(mob.id==1&&mob.health==10&&store.Count==1,"mob spawns with ten health and a persistent identity");
  Check(MobRules.Bounds(mob).size.x==.9f&&MobRules.Bounds(mob).size.y==1.3f,"classic mob collision size");
  var capture=store.Capture();capture[0].position.x=900;
  Check(mob.position.x==2,"mob save snapshot does not alias live position");
  var save=new MobSaveData{animals=store.Capture(),nextId=store.NextId,populatedChunks=new[]{new Vector3Int(-16,0,16)},randomState=12345,hasRandomState=true};
  var restored=new MobStore();restored.Restore(JsonUtility.FromJson<MobSaveData>(JsonUtility.ToJson(save)));
  Check(restored.Get(1).position.z==-3&&restored.Get(1).yaw==90,"mob identity and transform survive serialization");
  Check(restored.Remove(1)&&!restored.Remove(1)&&restored.Spawn(Vector3.zero,0).id==2,"removal happens once and saved identities are never reused");
  Check(!MobRules.Valid(new MobState{id=8,position=new Vector3(float.NaN,0,0)}),"invalid mob positions are rejected");
  restored.Restore(new MobSaveData{animals=new[]{new MobState{id=3,health=0},new MobState{id=4},new MobState{id=4}}});
  Check(restored.Count==1&&restored.NextId==5,"restoring rejects dead and duplicate mobs");
  var bounded=new MobStore();
  for(int i=0;i<MobStore.Capacity;i++)Check(bounded.Spawn(Vector3.zero,0)!=null,"bounded mob store accepts valid capacity");
  Check(bounded.Spawn(Vector3.zero,0)==null&&bounded.Count==MobStore.Capacity,"mob capacity prevents unbounded population");
  mob=new MobState{id=1};
  Check(MobRules.Hurt(mob,1,new Vector3(1,0,0))&&mob.health==9&&mob.fleeTicks==100&&mob.motion.y>0,"punch damages mob and starts flee/knockback");
  Check(!MobRules.Hurt(mob,1,Vector3.zero)&&mob.health==9,"hurt cooldown prevents repeated equal damage");
  Check(MobRules.Hurt(mob,3,Vector3.zero)&&mob.health==7,"a stronger hit during resistance applies only the damage difference");
  mob.hurtCooldown=0;Check(MobRules.Hurt(mob,10,Vector3.zero)&&mob.health==0&&!MobRules.Hurt(mob,1,Vector3.zero),"dead mobs cannot take damage again");
  Check(MobRules.AttackDamage(ItemType.Nothing)==1&&MobRules.AttackDamage(ItemType.WoodenSword)==4&&MobRules.AttackDamage(ItemType.StoneSword)==6&&MobRules.AttackDamage(ItemType.IronSword)==8&&MobRules.AttackDamage(ItemType.DiamondSword)==10,"Beta sword and hand damage");
  Check(MobRules.AttackDamage(ItemType.DiamondAxe)==6&&MobRules.AttackDamage(ItemType.DiamondPickaxe)==5&&MobRules.AttackDamage(ItemType.DiamondShovel)==4,"Beta tool attack damage");
  for(int roll=0;roll<3;roll++){int r=roll;Check(MobRules.LeatherCount(bound=>{Check(bound==3,"leather RNG bound");return r;})==roll,"Beta leather range includes zero");}
  var bag=new InventoryStack[36];bag[0]=new InventoryStack(ItemType.Bucket,1);
  Check(MobRules.Milk(bag,0)&&bag[0].type==ItemType.MilkBucket&&bag[0].count==1,"empty bucket becomes milk bucket in the selected slot");
  Check(!MobRules.Milk(bag,0)&&!MobRules.Milk(bag,-1),"milk cannot overwrite an unrelated item");
  bag=new InventoryStack[36];bag[0]=new InventoryStack(ItemType.Bucket,2);
  Check(MobRules.Milk(bag,0)&&bag[0].count==1&&bag.Count(s=>s.type==ItemType.MilkBucket)==1,"custom stacked buckets preserve remaining buckets");
  for(int i=0;i<36;i++)bag[i]=new InventoryStack(ItemType.Stone,64);
  bag[0]=new InventoryStack(ItemType.Bucket,2);
  Check(!MobRules.Milk(bag,0)&&bag[0].count==2,"full inventory refuses stacked milking without consuming a bucket");
  int vertices=0;
  for(int part=0;part<MobModelGeometry.PartCountFor(MobType.Cow);part++)
  {
   var model=MobModelGeometry.Build(part,32,MobType.Cow);vertices+=model.vertices.Count;
   Check(model.vertices.Count==24&&model.triangles.Count==36&&model.uv.Count==24,"each model box has six complete textured faces");
   foreach(var uv in model.uv)Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"mob UVs stay on the classic 64 by 32 skin");
   foreach(int triangle in model.triangles)Check(triangle>=0&&triangle<24,"mob triangle indices reference local box vertices");
   var center=new Vector3(model.vertices.Average(p=>p.x),model.vertices.Average(p=>p.y),model.vertices.Average(p=>p.z));
   for(int face=0;face<6;face++)
   {
    var a=model.vertices[face*4];var b=model.vertices[face*4+1];var c=model.vertices[face*4+2];
    var ab=b-a;var ac=c-a;var outward=a-center;
    var n=new Vector3(ab.y*ac.z-ab.z*ac.y,ab.z*ac.x-ab.x*ac.z,ab.x*ac.y-ab.y*ac.x);
    Check(n.x*outward.x+n.y*outward.y+n.z*outward.z>0,"mob winding faces outward for backface culling");
   }
  }
  Check(vertices==216,"mob includes body, head, two horns, udder and four legs");
  Check(MobModelGeometry.Pivot(5,MobType.Cow).x==-.25f&&MobModelGeometry.Pivot(7,MobType.Cow).z==.375f,"cow legs use classic widened and front-shifted pivots");
  var head=MobModelGeometry.Build(1,32,MobType.Cow);
  Check(Math.Abs(head.uv[16].x-(13.9f/64))<.000001f&&Math.Abs(head.uv[16].y-(1-6.1f/32))<.000001f,"head front samples face strip rather than back-of-head texture");
 }
}
