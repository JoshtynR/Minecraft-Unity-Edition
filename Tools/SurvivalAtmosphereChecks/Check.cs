using System;
using System.Reflection;
using UnityEngine;
class Check {
 static int checks;static void Assert(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Player Player(){var p=new GameObject("player").AddComponent<Player>();p.world=p.CurrentWorld=new World();p.cam=new GameObject("camera").transform;p.cam.gameObject.AddComponent<Camera>();return p;}
 static void Main(){
  var fall=new FallTracker();Assert(fall.Step(10,true,false)==0,"initial spawn safe");fall.Step(9,false,false);fall.Step(7,false,false);Assert(fall.Step(7,true,false)==0,"three block fall safe");
  fall.Step(6,false,false);fall.Step(3,false,false);Assert(fall.Step(3,true,false)==1,"four blocks costs half heart");Assert(fall.Step(3,true,false)==0,"landing charged once");
  fall.Step(2,false,false);fall.Step(1,false,true);Assert(fall.Step(0,true,false)==0,"water/flight breaks fall chain");fall.Step(100,false,false);Assert(fall.Step(100,true,false)==0,"teleport resets fall");
  fall.Reset();fall.Step(100,true,false);for(int y=99;y>60;y--)fall.Step(y,false,false);Assert(fall.Step(60,true,false)==37,"long fall damage");
  Assert(SkyClock.Daylight(6000)>.99&&SkyClock.Daylight(18000)<.01,"noon and midnight light");Assert(Math.Abs(SkyClock.Angle(1000)-SkyClock.Angle(25000))<.001,"twenty minute cycle wraps");
  Time.time=100;World.Overworld=new World{worldTimeTicks=17000};var server=new WorldServer();Call(server,"InitializeWorldClock");Time.time=110;Assert(Math.Abs(server.WorldTicks-17200)<.001,"server resumes saved time and advances twenty ticks per second");
  foreach(var type in new[]{ItemType.Apple,ItemType.Bread,ItemType.MushroomStew,ItemType.RawPorkchop,ItemType.CookedPorkchop,ItemType.RawFish,ItemType.CookedFish,ItemType.Cookie,ItemType.GoldenApple}){int f;float s;Assert(FoodRules.TryGet(type,out f,out s)&&f>0&&s>0,"food supported "+type);}
  Time.time=0;var p=Player();p.inventory.slots[0]=new InventoryStack{type=ItemType.Bread,count=2};Assert(p.StartFood()&&p.inventory.DropRequestPending,"eating starts and locks stack mutations");Time.time=1;p.EndFood(true);Assert(p.hunger==10&&p.inventory.slots[0].count==2,"early completion rejected");
  Assert(p.StartFood(),"retry food");Time.time=3;p.EndFood(true);Assert(p.hunger==15&&Math.Abs(p.saturation-6)<.001&&p.inventory.slots[0].count==1,"server applies nutrition and consumes once");p.EndFood(true);Assert(p.inventory.slots[0].count==1,"duplicate completion cannot duplicate food");
  p.StartFood();Time.time=5;p.EndFood(false);Assert(p.hunger==15&&p.inventory.slots[0].count==1&&!p.inventory.DropRequestPending,"release cancels cleanly");
  p.inventory.slots[0]=new InventoryStack{type=ItemType.MushroomStew,count=1};p.StartFood();Time.time=7;p.EndFood(true);Assert(p.hunger==20&&p.inventory.slots[0].type==ItemType.Bowl,"stew returns bowl and caps food");Assert(!p.StartFood(),"cannot eat at full hunger");
  p.hunger=10;p.inventory.slots[0]=new InventoryStack{type=ItemType.Apple,count=2};p.StartFood();p.inventory.selectedSlotIndex=1;p.FoodTick();Assert(!p.IsEating&&!p.inventory.DropRequestPending&&p.inventory.slots[0].count==2,"slot change cancels food");
  p.inventory.selectedSlotIndex=0;p.StartFood();p.knownItemWearRevision++;Time.time=10;p.EndFood(true);Assert(p.hunger==10,"stale inventory revision cannot consume");
  p.CameraTick();p.transform.position=new Vector3(.08f,0,0);p.CameraTick();Assert(p.cam.localPosition.y>0,"walking bobs camera");p.viewBobbing=false;for(int n=0;n<30;n++)p.CameraTick();Assert(Math.Abs(p.cam.localPosition.y)<.0001,"disabled bob returns to baseline");
  Resources.items["SkyMaterial"]=new Material(null);var sky=p.cam.GetComponent<OverworldAtmosphere>();sky.player=p;Call(sky,"Awake");Call(sky,"LateUpdate");
  Assert(GameObject.all.Exists(g=>g.name=="Sun")&&GameObject.all.Exists(g=>g.name=="Clouds"),"sky builds without source textures");Call(sky,"OnPreCull");Assert(Shader.GetGlobalVector("_WorldFogRange").z==1,"overworld fog enabled");Call(sky,"OnPostRender");Assert(Shader.GetGlobalVector("_WorldFogRange").z==0,"fog does not leak into UI/hand camera");
  p.CurrentWorld.dimension=WorldDimension.Nether;Call(sky,"LateUpdate");Assert(!GameObject.all.Find(g=>g.name=="Overworld sky visuals").activeSelf,"nether hides overworld sky");Call(sky,"OnDestroy");Assert(GameObject.all.Find(g=>g.name=="Overworld sky visuals").destroyed,"owned sky root released");
  Console.WriteLine(checks+" production food/fall/bob/sky lifecycle checks passed (Unity/Mirror mocked).");
 }
}
