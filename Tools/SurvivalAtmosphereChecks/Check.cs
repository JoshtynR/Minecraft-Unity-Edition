using System;
using System.Reflection;
using UnityEngine;
class Check {
 static int checks;static void Assert(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Player Player(){var p=new GameObject("player").AddComponent<Player>();p.world=p.CurrentWorld=new World();p.cam=new GameObject("camera").transform;p.cam.gameObject.AddComponent<Camera>();return p;}
 static void Main(){
  Assert(FoodRules.TryGet(ItemType.RawBeef,out int beefFood,out float beefSat)&&beefFood==3&&Math.Abs(beefSat-1.8f)<.001f,"raw beef nutrition");
  Assert(FoodRules.TryGet(ItemType.CookedBeef,out int steakFood,out float steakSat)&&steakFood==8&&Math.Abs(steakSat-12.8f)<.001f,"cooked beef nutrition");
  Assert(FoodRules.TryGet(ItemType.RawChicken,out int chickenFood,out float chickenSat)&&chickenFood==2,"raw chicken nutrition");
  Assert(FoodRules.TryGet(ItemType.CookedChicken,out int cookedFood,out float cookedSat)&&cookedFood==6,"cooked chicken nutrition");
  Assert(FoodRules.TryGet(ItemType.RottenFlesh,out int rottenFood,out float rottenSat)&&rottenFood==4,"rotten flesh base nutrition");
  var motion=new BetaPlayerMotion();motion.Advance(.2f,0,0,.05f,true,true,false);var one=motion.Sample(.05f);
  Assert(Math.Abs(one.X-Math.Sin(-.24*Math.PI)*.02)<.00001&&Math.Abs(one.Y+Math.Abs(Math.Cos(-.24*Math.PI)*.04))<.00001,"reference first camera-bob tick");
  Assert(Math.Abs(one.RightArm+Math.Cos(.32*.6662)*.32)<.00001&&Math.Abs(one.RightLeg-Math.Cos(.32*.6662)*.448)<.00001,"reference first model-limb tick");
  var reference=new BetaPlayerMotion();for(int n=0;n<100;n++)reference.Advance(4.317f/20,0,0,.05f,true,true,false);var expected=reference.Sample(.05f);
  foreach(int fps in new[]{30,50,60,144,240}){var other=new BetaPlayerMotion();for(int n=0;n<fps*5;n++)other.Advance(4.317f/fps,0,0,1f/fps,true,true,false);var actual=other.Sample(.05f);Assert(Math.Abs(actual.X-expected.X)<.0001&&Math.Abs(actual.RightArm-expected.RightArm)<.0001,"motion independent of sample rate "+fps);}
  for(int n=0;n<100;n++)motion.Advance(0,0,0,.05f,true,true,false);Assert(Math.Abs(motion.Sample(.05f).Y)<.00001&&Math.Abs(motion.Sample(.05f).RightLeg)<.00001,"stopped motion settles");
  motion.Advance(100,0,0,.05f,true,true,false);Assert(motion.Sample().Y==0,"teleport resets bob");
  motion.Advance(0,.2f,0,.05f,false,true,false);Assert(motion.Sample(.05f).Pitch<0,"jump camera pitches with vertical velocity");
  motion.Reset();motion.Advance(.05f,0,0,.05f,true,true,false,true);Assert(motion.Sample(.05f).X==0&&motion.Sample(.05f).RightLeg!=0,"grounded sneak keeps camera step phase but animates limbs");
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
  p.CameraTick();p.MotionTick(.05f);p.transform.position=new Vector3(.2f,0,0);p.MotionTick(.05f);p.CameraTick();Assert(p.cam.localPosition.y>0,"walking bobs camera");
  var held=new GameObject("held").transform;p.ApplyFirstPersonBob(held);Assert(held.localPosition.y<0,"held model gets forward view bob while camera gets inverse");
  p.viewBobbing=false;for(int n=0;n<30;n++)p.CameraTick();Assert(Math.Abs(p.cam.localPosition.y)<.0001,"disabled bob returns to baseline");
  var parts=new PlayerObjects{leftArm=new GameObject("leftArm").transform,rightArm=new GameObject("rightArm").transform,leftLeg=new GameObject("leftLeg").transform,rightLeg=new GameObject("rightLeg").transform};
  var walk=p.gameObject.AddComponent<MinecraftWalkAnimator>();walk.Initialize(p,parts);Call(walk,"LateUpdate");var left=parts.leftLeg.localRotation*Vector3.up;var right=parts.rightLeg.localRotation*Vector3.up;Assert(left.z*right.z<0,"walking legs swing in opposite directions");
  Resources.items["SkyMaterial"]=new Material(null);var sky=p.cam.GetComponent<OverworldAtmosphere>();sky.player=p;Call(sky,"Awake");Call(sky,"LateUpdate");
  Assert(GameObject.all.Exists(g=>g.name=="Sun")&&GameObject.all.Exists(g=>g.name=="Clouds"),"sky builds without source textures");Call(sky,"OnPreCull");Assert(Math.Abs(Shader.GetGlobalVector("_WorldFogRange").z-.62f)<.001f,"overworld fog uses published reduced strength");Call(sky,"OnPostRender");Assert(Shader.GetGlobalVector("_WorldFogRange").z==0,"fog does not leak into UI/hand camera");
  p.CurrentWorld.dimension=WorldDimension.Nether;Call(sky,"LateUpdate");Assert(!GameObject.all.Find(g=>g.name=="Overworld sky visuals").activeSelf,"nether hides overworld sky");Call(sky,"OnDestroy");Assert(GameObject.all.Find(g=>g.name=="Overworld sky visuals").destroyed,"owned sky root released");
  Console.WriteLine(checks+" production food/fall/bob/sky lifecycle checks passed (Unity/Mirror mocked).");
 }
}
